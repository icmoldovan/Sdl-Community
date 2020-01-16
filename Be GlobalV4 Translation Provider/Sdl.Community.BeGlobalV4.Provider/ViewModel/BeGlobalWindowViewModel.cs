﻿using System;
using System.Linq;
using System.Windows.Input;
using Sdl.Community.BeGlobalV4.Provider.Helpers;
using Sdl.Community.BeGlobalV4.Provider.Service;
using Sdl.Community.BeGlobalV4.Provider.Studio;
using Sdl.Community.BeGlobalV4.Provider.Ui;
using Sdl.LanguagePlatform.Core;
using Sdl.LanguagePlatform.TranslationMemoryApi;

namespace Sdl.Community.BeGlobalV4.Provider.ViewModel
{
	public class BeGlobalWindowViewModel : BaseViewModel
	{
		public BeGlobalTranslationOptions Options { get; set; }
		public LoginViewModel LoginViewModel { get; set; }
		public LanguageMappingsViewModel LanguageMappingsViewModel { get; set; }
		private ICommand _okCommand;
		private int _selectedTabIndex;
		private string _message;
		private Constants _constants = new Constants();
		private readonly BeGlobalWindow _mainWindow;
		public static readonly Log Log = Log.Instance;

		public BeGlobalWindowViewModel(BeGlobalWindow mainWindow, BeGlobalTranslationOptions options,
			TranslationProviderCredential credentialStore, LanguagePair[] languagePairs)
		{
			Options = options;	
			LanguageMappingsViewModel = new LanguageMappingsViewModel(options);

			LoginViewModel = new LoginViewModel(options, languagePairs, LanguageMappingsViewModel, this);
			_mainWindow = mainWindow;

			if (credentialStore == null) return;
			if (options.UseClientAuthentication || options.AuthenticationMethod.Equals("ClientLogin"))
			{
				_mainWindow.LoginTab.ClientIdBox.Password = options.ClientId;
				_mainWindow.LoginTab.ClientSecretBox.Password = options.ClientSecret;
				LoginViewModel.SelectedOption = LoginViewModel.AuthenticationOptions[0];
			}
			else
			{
				LoginViewModel.Email = options.ClientId;
				_mainWindow.LoginTab.UserPasswordBox.Password = options.ClientSecret;
				LoginViewModel.SelectedOption = LoginViewModel.AuthenticationOptions[1];
			}
		}

		public ICommand OkCommand => _okCommand ?? (_okCommand = new RelayCommand(Ok));

		public int SelectedTabIndex
		{
			get => _selectedTabIndex;
			set
			{
				Mouse.OverrideCursor = Cursors.Wait;
				_selectedTabIndex = value;
				LanguageMappingsViewModel.LoadLanguageMappings();
				ValidateWindow(true);				
				
				OnPropertyChanged();
				Mouse.OverrideCursor = Cursors.Arrow;
			}
		}
		
		public string Message
		{
			get => _message;
			set
			{
				if (_message == value)
				{
					return;
				}
				_message = value;
				OnPropertyChanged(nameof(Message));
			}
		}

		private void Ok(object parameter)
		{
			Mouse.OverrideCursor = Cursors.Wait;
			var loginTab = parameter as Login;
			if (loginTab != null)
			{
				var validateWindow = ValidateWindow(true);
				if (string.IsNullOrEmpty(validateWindow))
				{
					// Remove and add the new settings back to SettingsGroup of .sdlproj when user presses on Ok				
					LanguageMappingsViewModel.SaveLanguageMappingSettings();

					WindowCloser.SetDialogResult(_mainWindow, true);
					_mainWindow.Close();
				}
			}
			Mouse.OverrideCursor = Cursors.Arrow;
		}

		private string ValidateWindow(bool isOkPressed)
		{
			var loginTab = _mainWindow?.LoginTab;
			Options.ResendDrafts = LanguageMappingsViewModel.ReSendChecked;
			try
			{
				if(!LanguageMappingsViewModel.LanguageMappings.Any())
				{
					Message = _constants.EnginesSelectionMessage;
					return Message;
				}

				var areEnginesLoaded = LanguageMappingsViewModel.LanguageMappings.Any(l=>l.Engines.Any());
				if (LoginViewModel.SelectedOption.Type.Equals(_constants.User))
				{
					var password = loginTab?.UserPasswordBox.Password;
					if (!string.IsNullOrEmpty(password) && !string.IsNullOrEmpty(LoginViewModel.Email))
					{
						return GetEngineValidation(LoginViewModel?.Email, password, false, areEnginesLoaded, isOkPressed);
					}
				}
				else
				{
					var clientId = loginTab?.ClientIdBox.Password;
					var clientSecret = loginTab?.ClientSecretBox.Password;
					if (!string.IsNullOrEmpty(clientId) && !string.IsNullOrEmpty(clientSecret))
					{
						return GetEngineValidation(clientId, clientSecret, true, areEnginesLoaded, isOkPressed);
					}
				}
				if (loginTab != null)
				{
					Message = _constants.CredentialsValidation;
				}				
			}
			catch (Exception e)
			{
				if (loginTab != null)
				{
					Message = (e.Message.Contains(_constants.TokenFailed) || e.Message.Contains(_constants.NullValue)) ? _constants.CredentialsNotValid : e.Message;
				}
				Log.Logger.Error($"{_constants.IsWindowValid} {e.Message}\n {e.StackTrace}");
			}
			return Message;
		}

		private string GetEngineValidation(string clientId, string clientSecret, bool useClientAuthentication, bool areEnginesLoaded, bool isOkPressed)
		{
			Options.ClientId = clientId.TrimEnd().TrimStart();
			Options.ClientSecret = clientSecret.TrimEnd().TrimStart();
			Options.UseClientAuthentication = useClientAuthentication;
			Message = string.Empty;

			if (!areEnginesLoaded)
			{
				Message = _constants.NoEnginesLoaded;
				return Message;
			}
			if (isOkPressed)
			{
				var isEngineSetup = LoginViewModel.ValidateEnginesSetup();
				if (!isEngineSetup)
				{
					Message = _constants.CredentialsAndInternetValidation;
				}
				return Message;
			}
			return Message;
		}
	}
}