﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;
using System.IO;
using Binarysharp.MemoryManagement;
using Binarysharp.MemoryManagement.Memory;
using System.Linq;

namespace Anathema
{
    public partial class GUIPointerScanner : DockContent, IPointerScannerView
    {
        private PointerScannerPresenter PointerScannerPresenter;

        private const Int32 DefaultLevel = 3;
        private const Int32 DefaultOffset = 2048;

        public GUIPointerScanner()
        {
            InitializeComponent();

            PointerScannerPresenter = new PointerScannerPresenter(this, new PointerScanner());

            InitializeValueTypeComboBox();
            InitializeDefaults();
            EnableGUI();
        }

        private void InitializeDefaults()
        {
            MaxLevelTextBox.SetValue(DefaultLevel);
            MaxOffsetTextBox.SetValue(DefaultOffset);
        }

        private void InitializeValueTypeComboBox()
        {
            foreach (Type Primitive in PrimitiveTypes.GetPrimitiveTypes())
                ValueTypeComboBox.Items.Add(Primitive.Name);

            ValueTypeComboBox.SelectedIndex = ValueTypeComboBox.Items.IndexOf(typeof(Int32).Name);
        }

        public void DisplayScanCount(Int32 ScanCount) { }

        private void UpdateReadBounds()
        {
            ControlThreadingHelper.InvokeControlAction(PointerListView, () =>
            {
                Tuple<Int32, Int32> ReadBounds = PointerListView.GetReadBounds();
                PointerScannerPresenter.UpdateReadBounds(ReadBounds.Item1, ReadBounds.Item2);
            });
        }

        public void ScanFinished(Int32 ItemCount, Int32 MaxPointerLevel)
        {
            ControlThreadingHelper.InvokeControlAction<Control>(PointerListView, () =>
            {
                PointerListView.Items.Clear();

                // Remove offset columns
                while (PointerListView.Columns.Count > 2)
                    PointerListView.Columns.RemoveAt(2);

                // Create offset columns based on max level
                for (Int32 OffsetIndex = 0; OffsetIndex < MaxPointerLevel; OffsetIndex++)
                    PointerListView.Columns.Add("Offset " + OffsetIndex.ToString());

                PointerListView.VirtualListSize = ItemCount;
            });

            EnableGUI();
        }

        public void ReadValues()
        {
            UpdateReadBounds();

            ControlThreadingHelper.InvokeControlAction<Control>(PointerListView, () =>
            {
                PointerListView.BeginUpdate();
                PointerListView.EndUpdate();
            });
        }

        private void DisableGUI()
        {
            ControlThreadingHelper.InvokeControlAction<Control>(PointerListView, () =>
            {
                PointerListView.Enabled = false;
            });

            ControlThreadingHelper.InvokeControlAction<Control>(ScanToolStrip, () =>
            {
                StartScanButton.Enabled = false;
                StopScanButton.Enabled = true;
            });
        }

        private void EnableGUI()
        {
            ControlThreadingHelper.InvokeControlAction<Control>(PointerListView, () =>
            {
                PointerListView.Enabled = true;
            });

            ControlThreadingHelper.InvokeControlAction<Control>(ScanToolStrip, () =>
            {
                StartScanButton.Enabled = true;
                StopScanButton.Enabled = false;
            });
        }

        private void AddSelectedElements()
        {
            if (PointerListView.SelectedIndices.Count <= 0)
                return;

            PointerScannerPresenter.AddSelectionToTable(PointerListView.SelectedIndices[0], PointerListView.SelectedIndices[PointerListView.SelectedIndices.Count - 1]);
        }

        #region Events

        private void StartScanButton_Click(Object Sender, EventArgs E)
        {
            // Validate input
            if (!TargetAddressTextBox.IsValid() || !MaxLevelTextBox.IsValid() || !MaxOffsetTextBox.IsValid())
                return;

            // Apply settings
            PointerScannerPresenter.SetTargetAddress(TargetAddressTextBox.GetValueAsHexidecimal());
            PointerScannerPresenter.SetMaxPointerLevel(MaxLevelTextBox.GetValueAsDecimal());
            PointerScannerPresenter.SetMaxPointerOffset(MaxOffsetTextBox.GetValueAsDecimal());

            // Start scan
            DisableGUI();
            PointerScannerPresenter.BeginPointerScan();
        }

        private void RebuildPointersButton_Click(Object Sender, EventArgs E)
        {
            DisableGUI();

            if (TargetAddressTextBox.IsValid())
            {
                PointerScannerPresenter.SetTargetAddress(TargetAddressTextBox.GetValueAsHexidecimal());
            }

            PointerScannerPresenter.BeginPointerRescan();
        }

        private void StopScanButton_Click(Object Sender, EventArgs E)
        {
            EnableGUI();
        }

        private void PointerListView_RetrieveVirtualItem(Object Sender, RetrieveVirtualItemEventArgs E)
        {
            E.Item = PointerScannerPresenter.GetItemAt(E.ItemIndex);
        }

        private void ValueTypeComboBox_SelectedIndexChanged(Object Sender, EventArgs E)
        {
            TargetAddressTextBox.SetElementType(Conversions.StringToPrimitiveType(ValueTypeComboBox.SelectedItem.ToString()));
            PointerScannerPresenter.SetElementType(Conversions.StringToPrimitiveType(ValueTypeComboBox.SelectedItem.ToString()));
        }

        private void AddSelectedResultsButton_Click(Object Sender, EventArgs E)
        {
            AddSelectedElements();
        }

        private void PointerListView_DoubleClick(Object Sender, EventArgs E)
        {
            AddSelectedElements();
        }

        private void GUIPointerScanner_Resize(Object Sender, EventArgs E)
        {
            // Ensure tabs take up the entire width of the control
            const Int32 TabBoarderOffset = 3;
            PointerScanTabControl.ItemSize = new Size((PointerScanTabControl.Width - TabBoarderOffset) / PointerScanTabControl.TabCount, 0);
        }
        
        #endregion

    } // End class

} // End namespace