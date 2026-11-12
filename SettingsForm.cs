
using System;
using System.Drawing;
using System.Windows.Forms;

namespace AudioFocus
{
    public class SettingsForm : Form
    {
        private CheckBox alwaysPlayingBox;
        private NumericUpDown lockDelayInput;
        private NumericUpDown unlockDelayInput;

        public SettingsForm()
        {
            Text = "AudioFocus Settings";
            Size = new Size(350, 260);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;

            var settings = AudioFocus.GetSettings();

            alwaysPlayingBox = new CheckBox
            {
                Text = "Always Something Playing",
                Checked = settings.alwaysPlaying,
                Location = new Point(20, 20),
                AutoSize = true
            };

            var lockLabel = new Label
            {
                Text = "Lock Delay (ms)",
                Location = new Point(20, 65),
                AutoSize = true
            };

            lockDelayInput = new NumericUpDown
            {
                Location = new Point(180, 62),
                Width = 120,
                Minimum = 0,
                Maximum = 60000,
                Increment = 100,
                Value = Math.Clamp(settings.lockDelay, 0, 60000)
            };

            var unlockLabel = new Label
            {
                Text = "Unlock Delay (ms)",
                Location = new Point(20, 105),
                AutoSize = true
            };

            unlockDelayInput = new NumericUpDown
            {
                Location = new Point(180, 102),
                Width = 120,
                Minimum = 0,
                Maximum = 60000,
                Increment = 100,
                Value = Math.Clamp(settings.unlockDelay, 0, 60000)
            };

            var saveButton = new Button
            {
                Text = "Save",
                Location = new Point(130, 165),
                Width = 80
            };

            var cancelButton = new Button
            {
                Text = "Cancel",
                Location = new Point(220, 165),
                Width = 80,
                DialogResult = DialogResult.Cancel
            };

            saveButton.Click += (s, e) =>
            {
                AudioFocus.UpdateSettings(
                    alwaysPlayingBox.Checked,
                    (int)lockDelayInput.Value,
                    (int)unlockDelayInput.Value
                );

                Close();
            };

            Controls.AddRange(new Control[]
            {
                alwaysPlayingBox,
                lockLabel,
                lockDelayInput,
                unlockLabel,
                unlockDelayInput,
                saveButton,
                cancelButton
            });

            AcceptButton = saveButton;
            CancelButton = cancelButton;
        }
    }
}
