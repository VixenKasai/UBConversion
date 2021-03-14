using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace UBConversion {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        public MainWindow() {
            InitializeComponent();
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e) {
            this.Close();
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e) {
            OpenBrowser("http://discord.gg/PmmWAka");
        }

        public static void OpenBrowser(string url) {
            try {
                Process.Start(url);
            } catch {
                // hack because of this: https://github.com/dotnet/corefx/issues/10361
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                    url = url.Replace("&", "^&");
                    Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
                } else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
                    Process.Start("xdg-open", url);
                } else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
                    Process.Start("open", url);
                } else {
                    throw;
                }
            }
        }

        private void Instructions_Loaded(object sender, RoutedEventArgs e) {
            Instructions.Text = "1) Make an EUP outfit in-game and save it! (Make it a name you'll remember)\n\n" +
                "2) Go to GTAV\\Plugins\\EUP\\wardrobe.ini and find the section of the outfit you just made\n\n" +
                "3) Copy it and paste into the \"EUP\" section\n\n" +
                "4) Click \"Convert\" and copy the output from the \"UB\" section!\n\n" +
                "5) Place the text from the \"UB\" section it into your \"SpecialUnits\" or \"CustomRegions\" file wherever you want that ped to show up!\n\n" +
                "6) Don't forget your 'ped_chance=\"X\"' where applicatable\n\n" +
                "7) Enjoy!";
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e) {
            reset();
        }

        public void reset() {
            eupInput.Text = String.Empty;
            ubInput.Text = String.Empty;
            nameChkBox.IsChecked = false;
        }

        private void convertButton_Click(object sender, RoutedEventArgs e) {
            string input = eupInput.Text;
            bool includeName = nameChkBox.IsEnabled;
            string output = convertInput(input, includeName);
            ubInput.Text = output;
        }

        private string convertInput(string input, bool includeName) {
            try {
                //Init lookup Dictionaries
                Dictionary<string, string> components = new Dictionary<string, string>();
                Dictionary<string, string> translations = populateTranslations();

                //Populate the componant dictionary
                foreach (string str in input.Split("\n")) {
                    if (!str.Contains("=")) continue;
                    string[] parts = str.Split("=");
                    components.Add(parts[0], parts[1]);
                }

                //Find and save gender, then remove it as it is not a component
                string gender = components["Gender"];
                components.Remove("Gender");

                //Init string builder
                StringBuilder builder = new StringBuilder();

                //Include a comment in the output containing the name
                //of the outfit, if the user desires
                if (includeName) {
                    Regex regex = new Regex("(\\[.+\\])", RegexOptions.Compiled);
                    string uniformName = regex.Matches(input.Split("\n")[0])[0].Value;
                    builder.Append("<!-- " + uniformName + " --> ");
                }

                //open the tag
                builder.Append("<Ped ");

                //modify the tag with appropriate components
                /*
                * This works by utilizing the dictionaries.
                * It cycles through all components listed in the EUP config
                * For each config, it pulls the model and texture number
                * Then, utilizing the translation dictionary, it pulls the corresponding string to modify the tag
                * the ! and $ placeholders are replaced by the appropriate model and texture number 
                */
                foreach (String key in components.Keys) {
                    string model = components[key].Split(":")[0].Trim();
                    string texture = components[key].Split(":")[1].Trim();
                    string type = translations[key].Replace("!", model).Replace("$", texture);
                    builder.Append(type).Append(" ");
                }

                //Build the string and trim it to remove the trailing whitespace before assigning the ped model and closing the tag
                string allComps = builder.ToString().Trim();
                string genderType = gender.ToLower().Equals("female") ? "MP_F_FREEMODE_01" : "MP_M_FREEMODE_01";
                allComps += (">" + genderType + "</Ped>");

                //return the completed translation to be output to the user
                return allComps;
            } catch (KeyNotFoundException) {
                MessageBox.Show("Invalid EUP Form detected! Please check your input!");
                reset();
                return String.Empty;
            } catch (Exception) {
                MessageBox.Show("An unknown error occurred");
                return String.Empty;
            }
        }

        //translations for UB -- DO NOT TOUCH --
        public static Dictionary<string, string> populateTranslations() {
            Dictionary<string, string> translations = new Dictionary<string, string>();
            translations.Add("Hat", "prop_hats=\"!\" tex_hats=\"$\"");
            translations.Add("Glasses", "prop_glasses=\"!\"");
            translations.Add("Ear", "prop_ears=\"!\"");
            translations.Add("Watch", "prop_watches=\"!\"");
            translations.Add("Mask", "comp_beard=\"!\"");
            translations.Add("Top", "comp_shirtoverlay=\"!\" tex_shirtoverlay=\"$\"");
            translations.Add("UpperSkin", "comp_shirt=\"!\" tex_shirt=\"$\"");
            translations.Add("Decal", "comp_decals=\"!\" tex_decals=\"$\"");
            translations.Add("UnderCoat", "comp_accessories=\"!\" tex_accessories=\"$\"");
            translations.Add("Pants", "comp_pants=\"!\" tex_pants=\"$\"");
            translations.Add("Shoes", "comp_shoes=\"!\" tex_shoes=\"$\"");
            translations.Add("Accessories", "comp_eyes=\"!\" tex_eyes=\"$\"");
            translations.Add("Armor", "comp_tasks=\"!\" tex_tasks=\"$\"");
            translations.Add("Parachute", "comp_hands=\"!\" tex_hands=\"$\"");
            return translations;
        }

        private void eupInput_TextChanged(object sender, TextChangedEventArgs e) {
            ubInput.Text = String.Empty;
        }
    }
}

