﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using Sdl.Community.MTCloud.Languages.Provider;
using Sdl.Community.MTCloud.Provider.Commands;
using Sdl.Community.MTCloud.Provider.Helpers;
using Sdl.Community.MTCloud.Provider.Model;
using Sdl.Community.MTCloud.Provider.Studio;
using Sdl.Community.MTCloud.Provider.View;
using Sdl.LanguagePlatform.Core;
using Application = System.Windows.Forms.Application;
using Cursors = System.Windows.Input.Cursors;
using MessageBox = System.Windows.Forms.MessageBox;

namespace Sdl.Community.MTCloud.Provider.ViewModel
{
	public class OptionsViewModel : BaseViewModel, IDisposable
	{
		private readonly SdlMTCloudTranslationProvider _provider;
		private readonly List<LanguagePair> _languagePairs;

		private ICommand _saveCommand;
		private ICommand _resetToDefaultsCommand;
		private ICommand _viewLanguageMappingsCommand;

		private bool _reSendChecked;
		private LanguageMappingModel _selectedLanguageMapping;
		private ObservableCollection<LanguageMappingModel> _languageMappings;
		private bool _isWaiting;

		public OptionsViewModel(Window owner, SdlMTCloudTranslationProvider provider, List<LanguagePair> languagePairs)
		{
			Owner = owner;

			_provider = provider;
			_languagePairs = languagePairs;

			ReSendChecked = provider.Options?.ResendDraft ?? true;
			LoadLanguageMappings();
		}

		public ICommand SaveCommand => _saveCommand ?? (_saveCommand = new RelayCommand(Save));

		public ICommand ResetToDefaultsCommand => _resetToDefaultsCommand
														?? (_resetToDefaultsCommand = new RelayCommand(ResetToDefaults));

		public ICommand ViewLanguageMappingsCommand => _viewLanguageMappingsCommand
														?? (_viewLanguageMappingsCommand = new RelayCommand(ViewLanguageMappings));

		public Window Owner { get; }

		public ObservableCollection<LanguageMappingModel> LanguageMappings
		{
			get => _languageMappings;
			set
			{
				if (_languageMappings != null)
				{
					foreach (var languageMappingModel in _languageMappings)
					{
						languageMappingModel.PropertyChanged -= LanguageMappingModel_PropertyChanged;
					}
				}

				_languageMappings = value;

				if (_languageMappings != null)
				{
					foreach (var languageMappingModel in _languageMappings)
					{
						languageMappingModel.PropertyChanged += LanguageMappingModel_PropertyChanged;
					}
				}

				OnPropertyChanged(nameof(LanguageMappings));
			}
		}

		public LanguageMappingModel SelectedLanguageMapping
		{
			get => _selectedLanguageMapping;
			set
			{
				_selectedLanguageMapping = value;
				OnPropertyChanged(nameof(SelectedLanguageMapping));
			}
		}

		public bool ReSendChecked
		{
			get => _reSendChecked;
			set
			{
				if (_reSendChecked == value)
				{
					return;
				}

				_reSendChecked = value;
				OnPropertyChanged(nameof(ReSendChecked));
			}
		}

		public bool IsWaiting
		{
			get => _isWaiting;
			set
			{
				_isWaiting = value;
				OnPropertyChanged(nameof(IsWaiting));
			}
		}
		
		private void LoadLanguageMappings()
		{
			if (LanguageMappings != null && LanguageMappings.Any())
			{
				return;
			}			
			var languages = _provider.LanguageProvider.GetLanguages();
			var languageMappingModels = new List<LanguageMappingModel>();

			foreach (var languagePair in _languagePairs)
			{
				var languageMappingModel = _provider.GetLanguageMappingModel(languagePair, languages);
				if (languageMappingModel != null)
				{
					languageMappingModels.Add(languageMappingModel);
				}
			}		

			LanguageMappings = new ObservableCollection<LanguageMappingModel>(languageMappingModels);
		}

		private void ResetToDefaults(object parameter)
		{
			try
			{
				ReSendChecked = true;

				_provider.Options = new Options();				

				IsWaiting = true;
				if (Owner != null)
				{
					Mouse.OverrideCursor = Cursors.Wait;
				}

				if (LanguageMappings != null)
				{
					LanguageMappings.Clear();
					LoadLanguageMappings();

					if (Owner != null)
					{
						System.Windows.MessageBox.Show(PluginResources.Message_Successfully_reset_to_defaults,
							Application.ProductName, MessageBoxButton.OK, MessageBoxImage.Information);
					}
				}
			}
			catch (Exception ex)
			{
				IsWaiting = false;
				Log.Logger.Error($"{Constants.IsWindowValid} {ex.Message}\n {ex.StackTrace}");

				if (Owner != null)
				{
					Mouse.OverrideCursor = Cursors.Arrow;
					MessageBox.Show(ex.Message, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
				}
			}
			finally
			{
				IsWaiting = false;
				if (Owner != null)
				{
					Mouse.OverrideCursor = Cursors.Arrow;
				}
			}
		}

		private void Reload()
		{
			try
			{
				IsWaiting = true;
				if (Owner != null)
				{
					Mouse.OverrideCursor = Cursors.Wait;
				}

				if (LanguageMappings != null)
				{
					LanguageMappings.Clear();
					LoadLanguageMappings();
				}
			}
			catch (Exception ex)
			{
				IsWaiting = false;
				Log.Logger.Error($"{Constants.IsWindowValid} {ex.Message}\n {ex.StackTrace}");

				if (Owner != null)
				{
					Mouse.OverrideCursor = Cursors.Arrow;
					MessageBox.Show(ex.Message, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
				}
			}
			finally
			{
				IsWaiting = false;
				if (Owner != null)
				{
					Mouse.OverrideCursor = Cursors.Arrow;
				}
			}
		}

		private void Save(object parameter)
		{
			try
			{
				if (Owner != null)
				{
					Mouse.OverrideCursor = Cursors.Wait;
				}

				var canSave = true;
				var invalidModel = LanguageMappings.FirstOrDefault(a => a.SelectedModel.DisplayName == PluginResources.Message_No_model_available);
				if (invalidModel != null)
				{
					canSave = false;

					if (Owner != null)
					{
						var message = string.Format(PluginResources.Message_SelectLanguageDirectionForMTModel,
							invalidModel.SelectedSource.CodeName, invalidModel.SelectedTarget.CodeName);
						var question = PluginResources.Message_DoYouWantToProceed;

						var response = MessageBox.Show(message + Environment.NewLine + Environment.NewLine + question,
							Application.ProductName, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
						if (response == DialogResult.Yes)
						{
							canSave = true;
						}
					}
				}

				if (canSave)
				{
					_provider.Options.ResendDraft = ReSendChecked;
					
					_provider.Options.LanguageMappings = LanguageMappings.ToList();

					Dispose();

					if (Owner != null)
					{
						WindowCloser.SetDialogResult(Owner, true);
						Owner.Close();
					}
				}
			}
			finally
			{
				if (Owner != null)
				{
					Mouse.OverrideCursor = Cursors.Arrow;
				}
			}
		}

		private void ViewLanguageMappings(object obj)
		{
			if (Owner == null)
			{
				return;
			}

			var window = new MTCodesWindow
			{
				Owner = Owner.Owner
			};

			var languages = new LanguageProvider();
			var viewModel = new MTCodesViewModel(window, languages);
			window.DataContext = viewModel;

			var result = window.ShowDialog();
			if (result.HasValue && result.Value)
			{
				Reload();
			}
		}		

		private void LanguageMappingModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (sender is LanguageMappingModel languageModel)
			{
				if (e.PropertyName == nameof(LanguageMappingModel.SelectedSource) ||
					e.PropertyName == nameof(LanguageMappingModel.SelectedTarget))
				{
					UpdateAvailableSelection(languageModel);
				}
			}
		}

		private void UpdateAvailableSelection(LanguageMappingModel languageMappingModel)
		{
			var engineModels = _provider.LanguageMappingsService.GetTranslationModels(
				languageMappingModel.SelectedSource, languageMappingModel.SelectedTarget, languageMappingModel.SourceTradosCode,
				languageMappingModel.TargetTradosCode);

			if (engineModels.Any())
			{
				// assign the selected model
				var selectedModel =
					engineModels.FirstOrDefault(a => string.Compare(a.DisplayName,
						                                 languageMappingModel.SelectedModel?.DisplayName,
						                                 StringComparison.InvariantCultureIgnoreCase) == 0)
					?? engineModels.FirstOrDefault(a =>
						string.Compare(a.Model, "generic", StringComparison.InvariantCultureIgnoreCase) == 0)
					?? engineModels[0];

				languageMappingModel.Models = engineModels;
				languageMappingModel.SelectedModel = selectedModel;
			}

			var dictionaries =
				_provider.LanguageMappingsService.GetDictionaries(languageMappingModel.SelectedSource,
					languageMappingModel.SelectedTarget);

			// assign the selected dictionary
			var selectedDictionary =
				dictionaries.FirstOrDefault(a => a.Name.Equals(languageMappingModel.SelectedDictionary?.Name))
				?? dictionaries[0];

			languageMappingModel.Dictionaries = dictionaries;
			languageMappingModel.SelectedDictionary = selectedDictionary;
		}

		public void Dispose()
		{
			if (_languageMappings != null)
			{
				foreach (var languageMappingModel in _languageMappings)
				{
					languageMappingModel.PropertyChanged -= LanguageMappingModel_PropertyChanged;
				}
			}
		}
	}
}