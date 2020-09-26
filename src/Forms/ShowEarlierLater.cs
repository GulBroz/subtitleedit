﻿using Nikse.SubtitleEdit.Core;
using Nikse.SubtitleEdit.Core.Enums;
using Nikse.SubtitleEdit.Logic;
using System;
using System.Windows.Forms;

namespace Nikse.SubtitleEdit.Forms
{
    public sealed partial class ShowEarlierLater : PositionAndSizeForm
    {
        public delegate void AdjustEventHandler(double adjustMilliseconds, SelectionChoice selection);
        public delegate TimeCode DeltaVideoEventHandler();

        private TimeSpan _totalAdjustment;
        private AdjustEventHandler _adjustCallback;
        private DeltaVideoEventHandler _deltaCallback;

        public ShowEarlierLater()
        {
            UiUtil.PreInitialize(this);
            InitializeComponent();
            UiUtil.FixFonts(this);
            ResetTotalAdjustment();
            timeUpDownAdjust.MaskedTextBox.Text = "000000000";
            Text = Configuration.Settings.Language.ShowEarlierLater.Title.RemoveChar('&');
            labelHourMinSecMilliSecond.Text = Configuration.Settings.General.UseTimeFormatHHMMSSFF ? Configuration.Settings.Language.General.HourMinutesSecondsFrames : Configuration.Settings.Language.General.HourMinutesSecondsMilliseconds;
            buttonShowEarlier.Text = Configuration.Settings.Language.ShowEarlierLater.ShowEarlier;
            buttonShowLater.Text = Configuration.Settings.Language.ShowEarlierLater.ShowLater;
            radioButtonAllLines.Text = Configuration.Settings.Language.ShowEarlierLater.AllLines;
            radioButtonSelectedLinesOnly.Text = Configuration.Settings.Language.ShowEarlierLater.SelectedLinesOnly;
            radioButtonSelectedLineAndForward.Text = Configuration.Settings.Language.ShowEarlierLater.SelectedLinesAndForward;
            UiUtil.FixLargeFonts(this, buttonShowEarlier);

            timeUpDownAdjust.MaskedTextBox.TextChanged += (sender, args) =>
            {
                if (timeUpDownAdjust.GetTotalMilliseconds() < 0)
                {
                    timeUpDownAdjust.SetTotalMilliseconds(0);
                    System.Threading.SynchronizationContext.Current.Post(TimeSpan.FromMilliseconds(10), () =>
                    {
                        timeUpDownAdjust.SetTotalMilliseconds(0);
                    });
                }
            };
        }

        public void ResetTotalAdjustment()
        {
            _totalAdjustment = TimeSpan.FromMilliseconds(0);
            labelTotalAdjustment.Text = string.Empty;
        }

        private void ShowEarlierLater_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                Close();
            }
            else if (e.KeyCode == UiUtil.HelpKeys)
            {
                Utilities.ShowHelp("#sync");
                e.SuppressKeyPress = true;
            }
        }

        internal void Initialize(AdjustEventHandler adjustCallback, bool onlySelected, DeltaVideoEventHandler deltaCallback=null)
        {
            if (onlySelected)
            {
                radioButtonSelectedLinesOnly.Checked = true;
            }
            else if (Configuration.Settings.Tools.LastShowEarlierOrLaterSelection == SelectionChoice.SelectionAndForward.ToString())
            {
                radioButtonSelectedLineAndForward.Checked = true;
            }
            else
            {
                radioButtonAllLines.Checked = true;
            }

            _adjustCallback = adjustCallback;
            _deltaCallback = deltaCallback;
            timeUpDownAdjust.TimeCode = new TimeCode(Configuration.Settings.General.DefaultAdjustMilliseconds);
        }

        private SelectionChoice GetSelectionChoice()
        {
            if (radioButtonSelectedLinesOnly.Checked)
            {
                return SelectionChoice.SelectionOnly;
            }

            if (radioButtonSelectedLineAndForward.Checked)
            {
                return SelectionChoice.SelectionAndForward;
            }

            return SelectionChoice.AllLines;
        }

        private void ButtonShowEarlierClick(object sender, EventArgs e)
        {
            TimeCode tc = timeUpDownAdjust.TimeCode;
            if (tc != null && tc.TotalMilliseconds > 0)
            {
                _adjustCallback.Invoke(-tc.TotalMilliseconds, GetSelectionChoice());
                _totalAdjustment = TimeSpan.FromMilliseconds(_totalAdjustment.TotalMilliseconds - tc.TotalMilliseconds);
                ShowTotalAdjustMent();
                Configuration.Settings.General.DefaultAdjustMilliseconds = (int)tc.TotalMilliseconds;
            }
        }

        private void ShowTotalAdjustMent()
        {
            TimeCode tc = new TimeCode(_totalAdjustment);
            labelTotalAdjustment.Text = string.Format(Configuration.Settings.Language.ShowEarlierLater.TotalAdjustmentX, tc.ToShortString());
        }

        private void ButtonShowLaterClick(object sender, EventArgs e)
        {
            TimeCode tc = timeUpDownAdjust.TimeCode;
            if (tc != null && tc.TotalMilliseconds > 0)
            {
                _adjustCallback.Invoke(tc.TotalMilliseconds, GetSelectionChoice());
                _totalAdjustment = TimeSpan.FromMilliseconds(_totalAdjustment.TotalMilliseconds + tc.TotalMilliseconds);
                ShowTotalAdjustMent();
                Configuration.Settings.General.DefaultAdjustMilliseconds = (int)tc.TotalMilliseconds;
            }
        }

        private void RadioButtonCheckedChanged(object sender, EventArgs e)
        {
            Text = ((RadioButton)sender).Text.RemoveChar('&');
        }

        private void ShowEarlierLater_FormClosing(object sender, FormClosingEventArgs e)
        {
            Configuration.Settings.Tools.LastShowEarlierOrLaterSelection = GetSelectionChoice().ToString();
        }

        private void buttonVideoSync_Click(object sender, EventArgs e)
        {
            TimeCode delta = _deltaCallback?.Invoke();
            if (delta != null)
            {
                TimeCode reactiontime = timeUpDownAdjust.TimeCode;
                _adjustCallback.Invoke(delta.TotalMilliseconds - reactiontime.TotalMilliseconds, GetSelectionChoice());
                _totalAdjustment = TimeSpan.FromMilliseconds(_totalAdjustment.TotalMilliseconds + delta.TotalMilliseconds - reactiontime.TotalMilliseconds);
                ShowTotalAdjustMent();
                Configuration.Settings.General.DefaultAdjustMilliseconds = (int)reactiontime.TotalMilliseconds;
            }
        }
    }
}
