using System;
using System.Globalization;
using System.Threading;
using System.Windows;
using System.IO;
using System.Configuration;
using System.Collections.Generic;

namespace QuickTechPOS.Helpers
{
    public class LanguageManager
    {
        private static readonly string SettingsFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "QuickTechPOS",
            "language.config");

        public static List<LanguageOption> AvailableLanguages { get; } = new List<LanguageOption>
        {
            new LanguageOption { DisplayName = "English", Code = "en" },
            new LanguageOption { DisplayName = "العربية", Code = "ar" },
            new LanguageOption { DisplayName = "Français", Code = "fr" }
        };

        public static string CurrentLanguageCode { get; private set; } = "en";
        public static FlowDirection CurrentFlowDirection { get; private set; } = FlowDirection.LeftToRight;

        public static void Initialize()
        {
            LoadSavedLanguage();
            ApplyLanguage(CurrentLanguageCode);
        }

        public static void ChangeLanguage(string languageCode)
        {
            if (string.IsNullOrEmpty(languageCode) || languageCode == CurrentLanguageCode)
                return;

            ApplyLanguage(languageCode);
            SaveLanguagePreference(languageCode);
            CurrentLanguageCode = languageCode;
        }

        private static void ApplyLanguage(string languageCode)
        {
            // Create specific culture info
            CultureInfo culture = new CultureInfo(languageCode);

            // Set the current culture for the thread
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;

            // Update resource dictionaries
            ResourceDictionary resourceDictionary = new ResourceDictionary();

            try
            {
                switch (languageCode)
                {
                    case "ar":
                        resourceDictionary.Source = new Uri("/QuickTechPOS;component/Resources/StringResources.ar.xaml", UriKind.Relative);
                        // Arabic is RTL
                        CurrentFlowDirection = FlowDirection.RightToLeft;
                        break;
                    case "fr":
                        resourceDictionary.Source = new Uri("/QuickTechPOS;component/Resources/StringResources.fr.xaml", UriKind.Relative);
                        CurrentFlowDirection = FlowDirection.LeftToRight;
                        break;
                    default:
                        resourceDictionary.Source = new Uri("/QuickTechPOS;component/Resources/StringResources.xaml", UriKind.Relative);
                        CurrentFlowDirection = FlowDirection.LeftToRight;
                        break;
                }

                // Apply FlowDirection to MainWindow if it exists
                if (Application.Current?.MainWindow != null)
                {
                    Application.Current.MainWindow.FlowDirection = CurrentFlowDirection;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading resource dictionary: {ex.Message}");
                return;
            }

            // Get currently loaded resource dictionaries
            ResourceDictionary currentDict = null;
            foreach (ResourceDictionary dict in Application.Current.Resources.MergedDictionaries)
            {
                if (dict.Source != null && dict.Source.OriginalString.Contains("StringResources"))
                {
                    currentDict = dict;
                    break;
                }
            }

            // Replace or add the resource dictionary
            if (currentDict != null)
                Application.Current.Resources.MergedDictionaries.Remove(currentDict);

            Application.Current.Resources.MergedDictionaries.Add(resourceDictionary);
        }

        private static void SaveLanguagePreference(string languageCode)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(SettingsFilePath));
                File.WriteAllText(SettingsFilePath, languageCode);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving language preference: {ex.Message}");
            }
        }

        private static void LoadSavedLanguage()
        {
            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    string savedLanguage = File.ReadAllText(SettingsFilePath).Trim();
                    if (!string.IsNullOrEmpty(savedLanguage))
                    {
                        CurrentLanguageCode = savedLanguage;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading language preference: {ex.Message}");
            }
        }

        public static void ApplyFlowDirectionToWindow(Window window)
        {
            if (window != null)
            {
                window.FlowDirection = CurrentFlowDirection;
            }
        }

        public static LanguageOption GetCurrentLanguage()
        {
            return AvailableLanguages.Find(l => l.Code == CurrentLanguageCode)
                   ?? AvailableLanguages[0];
        }
    }

    public class LanguageOption
    {
        public string DisplayName { get; set; }
        public string Code { get; set; }

        public override string ToString()
        {
            return DisplayName;
        }
    }
}