/******************************************************************************
 * Star Wars: Episode I Racer Save Editor                                     *
 *                                                                            *
 * Copyright (C) 2021 J.C. Fields (jcfields@jcfields.dev).                    *
 *                                                                            *
 * This program is free software: you can redistribute it and/or modify it    *
 * under the terms of the GNU General Public License as published by the Free *
 * Software Foundation, either version 3 of the License, or (at your option)  *
 * any later version.                                                         *
 *                                                                            *
 * This program is distributed in the hope that it will be useful, but        *
 * WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY *
 * or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU General Public License   *
 * for more details.                                                          *
 *                                                                            *
 * You should have received a copy of the GNU General Public License along    *
 * with this program.  If not, see <http://www.gnu.org/licenses/>.            *
 ******************************************************************************/

using Microsoft.Win32; // for registry
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Ep1RacerSaveEditor
{
    public partial class MainForm : Form
    {
        enum Count
        {
            Profiles = 4,
            Circuits = 4,
            Tracks = 8,
            Positions = 2
        }

        private const string AppTitle = "Star Wars Episode I: Racer Save Editor";
        private const string FileFilter = "Star Wars Episode I: Racer Save File|*.dat";
        private const string ProfileFilter = "Star Wars Episode I: Racer Profile File|*.sav";
        private const string InstallDirectory = "Star Wars Episode I Racer";
        private const string SaveFileName = "tgfd.dat";

        private readonly string[,] TrackNames = {
            {
                "Boonta Training Course",
                "Mon Gazza Speedway",
                "Beedo's Wild Ride",
                "Aquilaris Classic",
                "Malastare 100",
                "Vengeance",
                "Spice Mine Run",
                "Unused"
            },
            {
                "Sunken City",
                "Howler Gorge",
                "Dug Derby",
                "Scrapper's Run",
                "Zugga Challenge",
                "Baroo Coast",
                "Bumpy's Breakers",
                "Unused"
            },
            {
                "Executioner",
                "Sebulba's Legacy",
                "Grabvine Gateway",
                "Andobi Mountain Run",
                "Dethro's Revenge",
                "Fire Mountain Rally",
                "The Boonta Classic",
                "Unused"
            },
            {
                "Ando Prime Centrum",
                "Abyss",
                "The Gauntlet",
                "Inferno",
                "Unused",
                "Unused",
                "Unused",
                "Unused"
            }
        };

        private readonly string[,] PartUpgrades =
        {
            {
                "R-20 Repulsorgrip",
                "R-60 Repulsorgrip",
                "R-80 Repulsorgrip",
                "R-100 Repulsorgrip",
                "R-300 Repulsorgrip",
                "R-600 Repulsorgrip"
            },
            {
                "Control Linkage",
                "Control Shift Plate",
                "Control Vectro-Jet",
                "Control Coupling",
                "Control Nozzle",
                "Control Stabilizer"
            },
            {
                "Dual 20 PCX Injector",
                "44 PCX Injector",
                "Dual 32 PCX Injector",
                "Quad 32 PCX Injector",
                "Quad 44 Injector",
                "Mag-6 Injector"
            },
            {
                "Plug2 Thrust Coil",
                "Plug3 Thrust Coil",
                "Plug5 Thrust Coil",
                "Plug8 Thrust Coil",
                "Block5 Thrust Coil",
                "Block6 Thrust Coil"
            },
            {
                "Mark II Air Brake",
                "Mark III Air Brake",
                "Mark IV Air Brake",
                "Mark V Air Brake",
                "Tri-Jet Air Brake",
                "Quadrijet Air Brake"
            },
            {
                "Coolant Radiator",
                "Stack-3 Radiator",
                "Stack-6 Radiator",
                "Rod Coolant Pump",
                "Dual Coolant Pump",
                "Turbo Coolant Pump"
            },
            {
                "Single Power Cell",
                "Dual Power Cell",
                "Quad Power Cell",
                "Cluster Power Plug",
                "Rotary Power Plug",
                "Cluster2 Power Plug"
            }
        };

        private SaveFile _saveFile;
        private bool _isModified;
        private bool _isLoaded; // form controls have been loaded (to differentiate user-fired events)
        private bool _isOpened; // file (besides default/blank file) is opened
        private int _selectedProfile;
        private int _selectedCircuit;
        private int _selectedTrack;

        public MainForm(string[] args)
        {
            InitializeComponent();

            _isModified = false;
            _isLoaded = false;
            _isOpened = false;

            SelectProfile(0);

            if (args.Length > 0)
            {
                LoadFile(args[0]);
            } else
            {
                LoadBlankFile();
            }

            _selectedCircuit = 0;
            _selectedTrack = 0;
    }

        private static string GetSteamPath()
        {
            var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Valve\Steam");

            // checks 64-bit registry if not found
            if (key == null)
            {
                key = RegistryKey.OpenBaseKey(
                        RegistryHive.LocalMachine,
                        RegistryView.Registry64
                    ).OpenSubKey(@"SOFTWARE\Valve\Steam");
            }

            string path = (string)key.GetValue("SteamPath");

            if (key == null || path == null)
            {
                throw new Exception("Could not locate path to Steam.");
            }

            return path.Replace("/", @"\");
        }

        private void LoadFile(string path)
        {
            try
            {
                _saveFile = new SaveFile(path);
                _isOpened = true;

                Reload();
                SetModified(false);
            }
            catch (Exception error)
            {
                MessageBox.Show(
                    error.Message,
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        private void LoadBlankFile()
        {
            _saveFile = new SaveFile();
            _isOpened = false;

            Reload();
            SetModified(false);
        }

        private void Reload()
        {
            if (_saveFile == null)
            {
                return;
            }

            _isLoaded = false;

            LoadPodracerPage();
            LoadTracksPage();
            LoadPartsPage();
            LoadTimesPage();

            LoadProfileNames();

            _isLoaded = true;
        }

        public void Save()
        {
            if (_saveFile.Path == "")
            {
                SaveAs();
            }
            else
            {
                try
                {
                    _saveFile.WriteFile();
                    SetModified(false);
                }
                catch (Exception error)
                {
                    MessageBox.Show(
                        error.Message,
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                }
            }
        }

        public void SaveAs()
        {
            var saveFileDialog = new SaveFileDialog
            {
                Filter = FileFilter,
                FileName = SaveFileName
            };

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    _saveFile.Path = saveFileDialog.FileName;
                    _saveFile.WriteFile();
                    _isOpened = true;

                    SetModified(false);
                }
                catch (Exception error)
                {
                    MessageBox.Show(
                        error.Message,
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                }
            }
        }

        private void CloseFile()
        {
            if (_isModified) // prompts for save if modified
            {
                string fileName = _saveFile.Path == "" ? "Untitled" : Path.GetFileName(_saveFile.Path);
                var dialogResult = MessageBox.Show(
                    $"Do you want to save changes to {fileName}?",
                    AppTitle,
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question
                );

                if (dialogResult == DialogResult.Yes)
                {
                    Save();
                }
            }
        }

        private void LoadPodracerPage()
        {
            var currentSlot = _saveFile[_selectedProfile];

            textBoxPlayerName.Text = currentSlot.PlayerName;
            comboBoxLastPodracer.SelectedIndex = currentSlot.LastPodracer;

            foreach (var control in tableLayoutPanelPodracers.Controls.OfType<CheckBox>())
            {
                int tag = Convert.ToInt32(control.Tag);
                control.Checked = currentSlot.Podracers[tag];
            }
        }
        private void LoadTracksPage()
        {
            var currentSlot = _saveFile[_selectedProfile];

            comboBoxTrackCircuit.SelectedIndex = _selectedCircuit;
            
            foreach (var control in tableLayoutPanelTracks.Controls.OfType<CheckBox>())
            {
                int tag = Convert.ToInt32(control.Tag);
                control.Enabled = tag > 0 || _selectedCircuit >= (int)Count.Circuits - 1;
                control.Text = TrackNames[_selectedCircuit, tag];
                control.Checked = currentSlot.Tracks[_selectedCircuit * (int)Count.Tracks + tag];
            }

            foreach (var control in tableLayoutPanelTracks.Controls.OfType<ComboBox>())
            {
                int tag = Convert.ToInt32(control.Tag);
                control.SelectedIndex = currentSlot.Ranks[_selectedCircuit * (int)Count.Tracks + tag];
            }
        }

        private void LoadPartsPage()
        {
            var currentSlot = _saveFile[_selectedProfile];

            numericUpDownTruguts.Value = currentSlot.Truguts;
            numericUpDownPitDroids.Value = currentSlot.PitDroids;

            foreach (var control in tableLayoutPanelParts.Controls.OfType<NumericUpDown>())
            {
                int tag = Convert.ToInt32(control.Tag);
                
                if (control.Name.StartsWith("numericUpDownLevel"))
                {
                    control.Value = currentSlot.PartLevels[tag];
                }
                else
                {
                    control.Value = currentSlot.PartHealth[tag];
                }
            }

            foreach (var control in tableLayoutPanelParts.Controls.OfType<Label>())
            {
                int tag = Convert.ToInt32(control.Tag);

                if (control.Name.StartsWith("labelLevel"))
                {
                    int level = currentSlot.PartLevels[tag];
                    control.Text = PartUpgrades[tag, level];
                }
            }
        }

        private void LoadTimesPage()
        {
            comboBoxTimesTrack.SelectedIndex = _selectedTrack;

            foreach (var control in tableLayoutPanelLapTime.Controls.OfType<NumericUpDown>())
            {
                int index = _selectedTrack * (int)Count.Positions + Convert.ToInt32(control.Tag);
                var time = _saveFile.LapTimes[index];

                if (control.Name.StartsWith("numericUpDownLapTimeMin"))
                {
                    control.Value = time.Min;
                } else if (control.Name.StartsWith("numericUpDownLapTimeSec"))
                {
                    control.Value = time.Sec;
                } else if (control.Name.StartsWith("numericUpDownLapTimeMsec"))
                {
                    control.Value = time.Msec;
                }
            }

            foreach (var control in tableLayoutPanelTotalTime.Controls.OfType<NumericUpDown>())
            {
                int index = _selectedTrack * (int)Count.Positions + Convert.ToInt32(control.Tag);
                var time = _saveFile.TotalTimes[index];

                if (control.Name.StartsWith("numericUpDownTotalTimeMin"))
                {
                    control.Value = time.Min;
                }
                else if (control.Name.StartsWith("numericUpDownTotalTimeSec"))
                {
                    control.Value = time.Sec;
                }
                else if (control.Name.StartsWith("numericUpDownTotalTimeMsec"))
                {
                    control.Value = time.Msec;
                }
            }

            foreach (var control in tableLayoutPanelLapTime.Controls.OfType<TextBox>())
            {
                int index = _selectedTrack * (int)Count.Positions + Convert.ToInt32(control.Tag);
                control.Text = _saveFile.LapNames[index];
            }

            foreach (var control in tableLayoutPanelTotalTime.Controls.OfType<TextBox>())
            {
                int index = _selectedTrack * (int)Count.Positions + Convert.ToInt32(control.Tag);
                control.Text = _saveFile.TotalNames[index];
            }

            foreach (var control in tableLayoutPanelLapTime.Controls.OfType<ComboBox>())
            {
                int index = _selectedTrack * (int)Count.Positions + Convert.ToInt32(control.Tag);
                control.SelectedIndex = _saveFile.LapPodracers[index];
            }

            foreach (var control in tableLayoutPanelTotalTime.Controls.OfType<ComboBox>())
            {
                int index = _selectedTrack * (int)Count.Positions + Convert.ToInt32(control.Tag);
                control.SelectedIndex = _saveFile.TotalPodracers[index];
            }
        }

        private void LoadProfileNames()
        {
            foreach (var control in profilesToolStripMenuItem.DropDownItems.OfType<ToolStripMenuItem>())
            {
                if (control.Name.StartsWith("profile"))
                {
                    int tag = Convert.ToInt32(control.Tag);
                    string name = _saveFile[tag].PlayerName;

                    control.Text = string.IsNullOrEmpty(name) ? string.Format("Profile &{0}", tag + 1) : name;
                }
            }
        }

        private void SelectProfile(int profile)
        {
            if (profile >= (int)Count.Profiles)
            {
                return;
            }

            _selectedProfile = profile;
            
            foreach (var control in profilesToolStripMenuItem.DropDownItems.OfType<ToolStripMenuItem>())
            {
                if (control.Name.StartsWith("profile"))
                {
                    control.Checked = profile == Convert.ToInt32(control.Tag);
                }
            }
        }

        private void SetModified(bool isModified)
        {
            if (_isLoaded)
            {
                if (isModified)
                {
                    if (!_isModified && _isOpened)
                    {
                        Text = '*' + Text;
                    } else
                    {
                        closeToolStripMenuItem.Enabled = true;
                    }
                    
                    saveToolStripMenuItem.Enabled = true;
                }
                else
                {
                    if (_isOpened)
                    {
                        Text = Path.GetFileName(_saveFile.Path) + " - " + AppTitle;
                        closeToolStripMenuItem.Enabled = true;
                    }
                    else
                    {
                        Text = AppTitle;
                        closeToolStripMenuItem.Enabled = false;
                    }

                    saveToolStripMenuItem.Enabled = false;
                }

                _isModified = isModified;
            }
        }

        private void SetProfile(object sender, EventArgs e)
        {
            var control = sender as ToolStripMenuItem;
            SelectProfile(Convert.ToInt32(control.Tag));

            Reload();
        }

        private void SetRank(object sender, EventArgs e)
        {
            if (_isLoaded)
            {
                var currentSlot = _saveFile[_selectedProfile];
                var control = sender as ComboBox;
                int index = _selectedCircuit * (int)Count.Tracks + Convert.ToInt32(control.Tag);
                currentSlot.Ranks[index] = control.SelectedIndex;

                SetModified(true);
            }
        }

        private void SetPartLevel(object sender, EventArgs e)
        {
            if (_isLoaded)
            {
                var currentSlot = _saveFile[_selectedProfile];
                var control = sender as NumericUpDown;
                int tag = Convert.ToInt32(control.Tag);
                currentSlot.PartLevels[tag] = (int)control.Value;

                SetModified(true);
            }
        }

        private void SetPartHealth(object sender, EventArgs e)
        {
            if (_isLoaded)
            {
                var currentSlot = _saveFile[_selectedProfile];
                var control = sender as NumericUpDown;
                int tag = Convert.ToInt32(control.Tag);
                currentSlot.PartHealth[tag] = (int)control.Value;

                SetModified(true);
            }
        }

        private void SetLapTimeMin(object sender, EventArgs e)
        {
            if (_isLoaded)
            {
                var control = sender as NumericUpDown;
                int index = _selectedTrack * (int)Count.Positions + Convert.ToInt32(control.Tag);
                _saveFile.LapTimes[index].Min = (int)control.Value;

                SetModified(true);
            }
        }

        private void SetLapTimeSec(object sender, EventArgs e)
        {
            if (_isLoaded)
            {
                var control = sender as NumericUpDown;
                int index = _selectedTrack * (int)Count.Positions + Convert.ToInt32(control.Tag);
                _saveFile.LapTimes[index].Sec = (int)control.Value;

                SetModified(true);
            }
        }

        private void SetLapTimeMsec(object sender, EventArgs e)
        {
            if (_isLoaded)
            {
                var control = sender as NumericUpDown;
                int index = _selectedTrack * (int)Count.Positions + Convert.ToInt32(control.Tag);
                _saveFile.LapTimes[index].Msec = (int)control.Value;

                SetModified(true);
            }
        }

        private void SetTotalTimeMin(object sender, EventArgs e)
        {
            if (_isLoaded)
            {
                var control = sender as NumericUpDown;
                int index = _selectedTrack * (int)Count.Positions + Convert.ToInt32(control.Tag);
                _saveFile.TotalTimes[index].Min = (int)control.Value;

                SetModified(true);
            }
        }

        private void SetTotalTimeSec(object sender, EventArgs e)
        {
            var control = sender as NumericUpDown;
            int index = _selectedTrack * (int)Count.Positions + Convert.ToInt32(control.Tag);
            _saveFile.TotalTimes[index].Sec = (int)control.Value;

            SetModified(true);
        }

        private void SetTotalTimeMsec(object sender, EventArgs e)
        {
            if (_isLoaded)
            {
                var control = sender as NumericUpDown;
                int index = _selectedTrack * (int)Count.Positions + Convert.ToInt32(control.Tag);
                _saveFile.TotalTimes[index].Msec = (int)control.Value;

                SetModified(true);
            }
        }

        private void SetLapTimeName(object sender, EventArgs e)
        {
            if (_isLoaded)
            {
                var control = sender as TextBox;
                int index = _selectedTrack * (int)Count.Positions + Convert.ToInt32(control.Tag);
                _saveFile.LapNames[index] = Regex.Replace(control.Text.ToUpper(), @"[^A-Z0-9 ]", "");
                SetModified(true);
            }
        }

        private void SetTotalTimeName(object sender, EventArgs e)
        {
            if (_isLoaded)
            {
                var control = sender as TextBox;
                int index = _selectedTrack * (int)Count.Positions + Convert.ToInt32(control.Tag);
                _saveFile.TotalNames[index] = Regex.Replace(control.Text.ToUpper(), @"[^A-Z0-9 ]", "");

                SetModified(true);
            }
        }

        private void SetLapTimePodracer(object sender, EventArgs e)
        {
            if (_isLoaded)
            {
                var control = sender as ComboBox;
                int index = _selectedTrack * (int)Count.Positions + Convert.ToInt32(control.Tag);
                _saveFile.LapPodracers[index] = control.SelectedIndex;

                SetModified(true);
            }
        }

        private void SetTotalTimePodracer(object sender, EventArgs e)
        {
            if (_isLoaded)
            {
                var control = sender as ComboBox;
                int index = _selectedTrack * (int)Count.Positions + Convert.ToInt32(control.Tag);
                _saveFile.TotalPodracers[index] = control.SelectedIndex;

                SetModified(true);
            }
        }

        private void TogglePodracer(object sender, EventArgs e)
        {
            if (_isLoaded)
            {
                var currentSlot = _saveFile[_selectedProfile];
                var control = sender as CheckBox;
                int tag = Convert.ToInt32(control.Tag);
                currentSlot.Podracers[tag] = control.Checked;

                SetModified(true);
            }
        }

        private void ToggleTrack(object sender, EventArgs e)
        {
            if (_isLoaded)
            {
                var currentSlot = _saveFile[_selectedProfile];
                var control = sender as CheckBox;
                int index = _selectedCircuit * (int)Count.Tracks + Convert.ToInt32(control.Tag);
                currentSlot.Tracks[index] = control.Checked;

                SetModified(true);
            }
        }

        private void ClickOpenButton(object sender, EventArgs e)
        {
            CloseFile();

            var openFileDialog = new OpenFileDialog
            {
                Filter = FileFilter
            };

            if (openFileDialog.ShowDialog(this) == DialogResult.OK)
            {
                LoadFile(openFileDialog.FileName);
            }
        }

        private void ClickOpenSteamButton(object sender, EventArgs e)
        {
            try
            {
                string steamPath = GetSteamPath();

                if (steamPath != null)
                {
                    string path = Path.Combine(
                        steamPath,
                        "steamapps",
                        "common",
                        InstallDirectory,
                        "data",
                        "player",
                        SaveFileName
                    );

                    CloseFile();
                    LoadFile(path);
                }
            }
            catch (Exception error)
            {
                MessageBox.Show(
                    error.Message,
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        private void ClickCloseButton(object sender, EventArgs e)
        {
            CloseFile();
            LoadBlankFile();
        }

        private void ClickSaveButton(object sender, EventArgs e)
        {
            Save();
        }

        private void ClickSaveAsButton(object sender, EventArgs e)
        {
            SaveAs();
        }

        private void ClickExitButton(object sender, EventArgs e)
        {
            CloseFile();
            Close();
        }

        private void ClickImportButton(object sender, EventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = ProfileFilter
            };

            if (openFileDialog.ShowDialog(this) == DialogResult.OK)
            {
                try
                {
                    _saveFile.ReplaceSlot(openFileDialog.FileName, _selectedProfile);

                    Reload();
                    SetModified(true);
                }
                catch (Exception error)
                {
                    MessageBox.Show(
                        error.Message,
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                }
            }
        }

        private void ClickExportButton(object sender, EventArgs e)
        {
            var currentSlot = _saveFile[_selectedProfile];
            string name = currentSlot.PlayerName;

            var saveFileDialog = new SaveFileDialog
            {
                Filter = ProfileFilter,
                FileName = string.IsNullOrEmpty(name) ? string.Format("Profile {0}", _selectedProfile + 1) : name
            };

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    currentSlot.WriteFile(saveFileDialog.FileName);
                }
                catch (Exception error)
                {
                    MessageBox.Show(
                        error.Message,
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                }
            }
        }

        private void ClickResetButton(object sender, EventArgs e)
        {
            var dialogResult = MessageBox.Show(
                "Are you sure you want to reset this profile slot to its default values?",
                AppTitle,
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (dialogResult == DialogResult.Yes)
            {
                _saveFile.ResetSlot(_selectedProfile);

                Reload();
                SetModified(true);
            }
        }

        private void ClickVisitWebSiteButton(object sender, EventArgs e)
        {
            var form = new AboutBox();
            form.OpenWebSite();
        }

        private void ClickAboutButton(object sender, EventArgs e)
        {
            var form = new AboutBox();
            form.ShowDialog();
        }

        private void ChangePlayerNameText(object sender, EventArgs e)
        {
            if (_isLoaded)
            {
                var currentSlot = _saveFile[_selectedProfile];
                var control = sender as TextBox;
                // removing special characters here instead of the slot class avoids cursor positioning problems
                currentSlot.PlayerName = Regex.Replace(control.Text.ToUpper(), @"[^A-Z0-9 ]", "");

                int cursorPosition = control.SelectionStart;
                control.Text = currentSlot.PlayerName;
                control.SelectionStart = cursorPosition;

                SetModified(true);
            }
        }

        private void ChangeLastPodracerSelect(object sender, EventArgs e)
        {
            if (_isLoaded)
            {
                var currentSlot = _saveFile[_selectedProfile];
                var control = sender as ComboBox;
                currentSlot.LastPodracer = control.SelectedIndex;

                SetModified(true);
            }
        }

        private void ChangeTrackCircuitSelect(object sender, EventArgs e)
        {
            var control = sender as ComboBox;
            _selectedCircuit = control.SelectedIndex;

            _isLoaded = false; // prevents values from being written when not changed by user
            LoadTracksPage();
            _isLoaded = true;
        }

        private void ChangeTrugutsValue(object sender, EventArgs e)
        {
            if (_isLoaded)
            {
                var currentSlot = _saveFile[_selectedProfile];
                var control = sender as NumericUpDown;
                currentSlot.Truguts = (int)control.Value;

                SetModified(true);
            }
        }

        private void ChangePitDroidsValue(object sender, EventArgs e)
        {
            if (_isLoaded)
            {
                var currentSlot = _saveFile[_selectedProfile];
                var control = sender as NumericUpDown;
                currentSlot.PitDroids = (int)control.Value;

                SetModified(true);
            }
        }

        private void ChangeTimesTrackSelect(object sender, EventArgs e)
        {
            var control = sender as ComboBox;

            _isLoaded = false; // prevents values from being written when not changed by user
            _selectedTrack = control.SelectedIndex;
            LoadTimesPage();
            _isLoaded = true;
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            CloseFile();
        }
    }
}
