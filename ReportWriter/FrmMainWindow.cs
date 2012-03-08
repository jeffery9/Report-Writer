﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace Report_Writer
{
	public partial class FrmMainWindow : Form
	{
		DocumentLib.Parser parser = new DocumentLib.Parser();
		string filePath;

		public FrmMainWindow()
		{
			InitializeComponent();
		}

		private void FrmMainWindow_Load(object sender, EventArgs e)
		{
			Open(Properties.Settings.Default.LastDocument);
		}

		private void txtDocument_KeyUp(object sender, KeyEventArgs e)
		{
			if (e.KeyCode != Keys.Enter && e.KeyCode != Keys.Up && e.KeyCode != Keys.Down)
				return;

			UpdateInterface();
		}

		private void txtDocument_TextChanged(object sender, EventArgs e)
		{
			changed = true;
			needSave = true;
		}

		private void lbNavigation_SelectedIndexChanged(object sender, EventArgs e)
		{
			DocumentLib.Chapter chapter = (DocumentLib.Chapter)lbNavigation.SelectedItem;

			if (chapter == null)
				return;

			Navigate(chapter.position);
		}

		private void UpdateInterface()
		{
			if (changed == false)
				return;

			parser.SetDocument(txtDocument.Text, Path.GetDirectoryName(filePath));
			parser.Parse();

			lbLog.Items.Clear();

			foreach (DocumentLib.LogLine l in parser.GetLog())
				lbLog.Items.Add(l);

			lbNavigation.Items.Clear();
			lbFigures.Items.Clear();
			lbTables.Items.Clear();
			lbReferences.Items.Clear();
			lbTodos.Items.Clear();

			foreach (KeyValuePair<string, DocumentLib.Chapter> pair in parser.GetChapters())
			{
				// BackColorText(pair.Value.position, pair.Value.match.Length, Color.LightGreen);
				lbNavigation.Items.Add(pair.Value);
			}

			foreach (KeyValuePair<string, DocumentLib.Figure> pair in parser.GetFigures())
			{
				// BackColorText(pair.Value.position, pair.Value.match.Length, Color.Yellow);
				lbFigures.Items.Add(pair.Value);
			}

			foreach (KeyValuePair<string, DocumentLib.Table> pair in parser.GetTables())
			{
				// BackColorText(pair.Value.position, pair.Value.match.Length, Color.Yellow);
				lbTables.Items.Add(pair.Value);
			}

			foreach (DocumentLib.Reference r in parser.GetReferences())
			{
				lbReferences.Items.Add(r);
			}

			foreach (Match m in new Regex("^(TODO|NOTE):.*", RegexOptions.Multiline).Matches(txtDocument.Text))
				lbTodos.Items.Add(new DocumentLib.Todo(m.Index, m.Value));

			changed = false;
		}

		private void Navigate(int position)
		{
			txtDocument.Focus();

			txtDocument.Select(txtDocument.Text.Length, 0);
			txtDocument.ScrollToCaret();

			txtDocument.Select(position, 0);
			txtDocument.ScrollToCaret();
		}

		private bool changed = false;
		private bool needSave = false;

		private void txtDocument_MouseUp(object sender, MouseEventArgs e)
		{
			UpdateInterface();
		}

		private void lbLog_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (lbLog.SelectedItem != null)
				Navigate(((DocumentLib.LogLine)lbLog.SelectedItem).position);

			lbLog.ClearSelected();
		}

		private void lbFigures_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (lbFigures.SelectedItem != null)
				Navigate(((DocumentLib.Figure)lbFigures.SelectedItem).position);

			lbFigures.ClearSelected();
		}

		private void lbTables_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (lbTables.SelectedItem != null)
				Navigate(((DocumentLib.Table)lbTables.SelectedItem).position);

			lbTables.ClearSelected();
		}

		private void lbReferences_SelectedIndexChanged(object sender, EventArgs e)
		{
			lbReferences.ClearSelected();
		}

		private void BackColorText(int start, int length, Color color)
		{
			this.ActiveControl = null;

			int currentPosition = txtDocument.SelectionStart;

			txtDocument.Select(start, length);
			txtDocument.SelectionBackColor = color;

			txtDocument.Select(currentPosition, 0);

			txtDocument.Focus();
		}

		private void Open(string path)
		{
			if (!File.Exists(path))
				return;

			DocumentClose();

			filePath = path;

			txtDocument.Text = "";
			txtDocument.Text = File.ReadAllText(path);
			tsslblTip.Text = "Opened document '" + filePath + "'";

			Properties.Settings.Default.LastDocument = filePath;
			Properties.Settings.Default.Save();

			UpdateInterface();

			this.Text = "ReportWriter - " + Path.GetFileName(filePath);

			needSave = false;
		}

		private void Save()
		{
			UpdateInterface();

			File.WriteAllText(filePath, txtDocument.Text.Replace("\n", Environment.NewLine));
			tsslblTip.Text = "Saved document '" + filePath + "'";

			needSave = false;
		}

		private void Export()
		{
			Save();
			UpdateInterface();

			DocumentLib.Parser fullParser = new DocumentLib.Parser();
			fullParser.SetDocument(txtDocument.Text, Path.GetDirectoryName(filePath));
			fullParser.Parse(true);

			string exportName = Path.Combine(Path.GetDirectoryName(filePath), Path.GetFileName(filePath).Replace(".txt", ".html"));
			string html = DocumentLib.HtmlGenerator.GetHtml(fullParser);

			File.WriteAllText(exportName, html);

			tsslblTip.Text = "Exported document to '" + exportName + "'";
		}

		private void tsbOpen_Click(object sender, EventArgs e)
		{
			DocumentClose();

			if (ofdOpen.ShowDialog() != DialogResult.OK)
				return;

			Open(ofdOpen.FileName);
		}

		private void tsbSave_Click(object sender, EventArgs e)
		{
			Save();
		}

		private void tsbExport_Click(object sender, EventArgs e)
		{
			Export();
		}

		private void txtDocument_KeyDown(object sender, KeyEventArgs e)
		{
			bool handled = false;

			if ((e.KeyCode == Keys.O || e.KeyValue == 131087) && e.Control)
			{
				if (ofdOpen.ShowDialog() != DialogResult.OK)
					return;

				Open(ofdOpen.FileName);

				handled = true;
			}

			if ((e.KeyCode == Keys.S || e.KeyValue == 131091) && e.Control)
			{
				Save();
				handled = true;
			}

			if ((e.KeyCode == Keys.E || e.KeyValue == 131077) && e.Control)
			{
				Export();
				handled = true;
			}

			if ((e.KeyCode == Keys.C || e.KeyValue == 131075) && e.Control)
			{
				if (txtDocument.SelectedText != "")
				{
					Clipboard.Clear();
					Clipboard.SetText(txtDocument.SelectedText);
				}

				handled = true;
			}

			if ((e.KeyCode == Keys.V || e.KeyValue == 131094) && e.Control)
			{
				txtDocument.SelectedText = Clipboard.GetText();
				handled = true;
			}

			if (handled)
				e.SuppressKeyPress = true;
		}

		private void FrmMainWindow_FormClosing(object sender, FormClosingEventArgs e)
		{
			if (DocumentClose() == DialogResult.Cancel)
				e.Cancel = true;
		}

		private DialogResult DocumentClose()
		{
			DialogResult res = DialogResult.No;

			if (needSave)
			{
				res = MessageBox.Show("Do you want to save changes you made to the document?", "ReportWriter", MessageBoxButtons.YesNoCancel);

				if (res == DialogResult.Yes)
					Save();
			}

			return res;
		}

		private void lbTodos_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (lbTodos.SelectedItem == null)
				return;

			DocumentLib.Todo todo = (DocumentLib.Todo)lbTodos.SelectedItem;

			Navigate(todo.location);

			lbTodos.ClearSelected();
		}
	}
}
