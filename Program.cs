using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;
using System.Drawing;

public class UrlEntry
{
    public string Name { get; set; }
    public string Url { get; set; } // Only used if no sub-buttons
    public string Subtitle { get; set; }
    public List<SubButtonEntry> SubButtons { get; set; }
}

public class SubButtonEntry
{
    public string Name { get; set; }
    public string Url { get; set; }
    public string Subtitle { get; set; }
}

class Program : Form
{
    private readonly Dictionary<string, string> tabFiles = new Dictionary<string, string>
{
    { "Games", "https://pastebin.com/raw/82qG4QKW" },
    { "Online Games", "https://pastebin.com/raw/E9USbbDM" },
    { "Other", "https://pastebin.com/raw/z6DDsVJa" },
    { "Cracking Tools", "https://pastebin.com/raw/1VAugM38" }
};


    private TabControl tabControl;
    private Dictionary<string, Panel> tabPanels = new Dictionary<string, Panel>();
    private Dictionary<string, string> lastContents = new Dictionary<string, string>();
    private System.Timers.Timer updateTimer;
    private const string CurrentVersion = "1.1.0"; // your app's version
    private const string LatestVersionUrl = "https://raw.githubusercontent.com/InsiderANN/Game-Puller/main/version.txt";
    private const string GitHubReleasesUrl = "https://github.com/InsiderANN/Game-Puller/releases/tag/Release";


    [STAThread]
    static void Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "-dev")
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new JsonMaker());
        }
        else
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            var app = new Program();
            bool upToDate = Task.Run(() => app.CheckForUpdates()).Result;
            if (!upToDate) return;
            Application.Run(app);
        }
    }

    public Program()
    {
        this.Text = "Cracked Game URLs";
        this.Width = 600;
        this.Height = 800;

        tabControl = new TabControl() { Dock = DockStyle.Fill };
        this.Controls.Add(tabControl);

        foreach (var tab in tabFiles.Keys)
        {
            TabPage page = new TabPage(tab);
            Panel panel = new Panel() { Dock = DockStyle.Fill, AutoScroll = true };
            page.Controls.Add(panel);
            tabControl.TabPages.Add(page);
            tabPanels[tab] = panel;
            lastContents[tab] = "";
        }

        updateTimer = new System.Timers.Timer(5000);
        updateTimer.AutoReset = true;
        updateTimer.Elapsed += async (s, e) => await UpdateAllTabs();
        updateTimer.Start();

        Task.Run(async () => await UpdateAllTabs());
    }

private async Task UpdateAllTabs()
{
    foreach (var tab in tabFiles.Keys)
    {
        await UpdateTab(tab, tabFiles[tab]);
    }
}

private async Task UpdateTab(string tabName, string url)
{
    try
    {
        using (HttpClient client = new HttpClient())
        {
            string content = await client.GetStringAsync(url);
            if (content == lastContents[tabName]) return;
            lastContents[tabName] = content;

            var entries = JsonSerializer.Deserialize<List<UrlEntry>>(content);

            this.Invoke((Action)(() =>
            {
                Panel panel = tabPanels[tabName];
                panel.Controls.Clear();
                int top = 10;

                foreach (var entry in entries)
                {
                    CreateMainButton(panel, entry, ref top);
                }

                // Force all sub-panels to auto-size
                foreach (Control ctrl in panel.Controls)
                {
                    if (ctrl is Panel subPanel)
                    {
                        subPanel.Visible = true; // optional: expand at start
                        subPanel.PerformLayout();
                    }
                }

                // Recalculate all button positions based on real sizes
                AdjustAllButtonPositions(panel);
            }));
        }
    }
    catch
    {
        // Silently fail
    }
}
private void CreateMainButton(Panel panel, UrlEntry entry, ref int top)
{
    // Main button
    Button mainBtn = new Button()
    {
        Text = entry.Name,
        Width = panel.Width - 40,
        Height = 40,
        Left = 10,
        Top = top
    };
    panel.Controls.Add(mainBtn);
    top += mainBtn.Height + 5;

    // Sub-panel for subbuttons
    Panel subPanel = new Panel()
    {
        Width = mainBtn.Width,
        Left = mainBtn.Left,
        Top = top,
        AutoSize = true,
        Visible = true // start collapsed
    };
    panel.Controls.Add(subPanel);

    int subTop = 0;

    if (entry.SubButtons != null)
    {
        foreach (var sb in entry.SubButtons)
        {
            Button subBtn = new Button()
            {
                Text = sb.Name,
                Width = subPanel.Width - 20,
                Height = 30,
                Left = 10,
                Top = subTop
            };
            string url = sb.Url;
            subBtn.Click += (s, e) => OpenUrl(url);
            subPanel.Controls.Add(subBtn);
            subTop += subBtn.Height + 2;

            if (!string.IsNullOrEmpty(sb.Subtitle))
            {
                TextBox subTxt = new TextBox()
                {
                    Text = sb.Subtitle,
                    Left = subBtn.Left,
                    Top = subTop,
                    Width = subBtn.Width,
                    Height = 25,
                    ReadOnly = true
                };
                subPanel.Controls.Add(subTxt);
                subTop += subTxt.Height + 2;
            }
        }
    }

    // Main button click
    mainBtn.Click += (s, e) =>
    {
        if (entry.SubButtons == null || entry.SubButtons.Count == 0)
        {
            // Open URL directly if no subbuttons
            if (!string.IsNullOrEmpty(entry.Url))
                OpenUrl(entry.Url);
        }
        else
        {
            // Toggle sub-panel
            subPanel.Visible = !subPanel.Visible;
            AdjustPositionsFrom(panel, mainBtn);
        }
    };

    // Optional main subtitle (only if no subbuttons)
    if (!string.IsNullOrEmpty(entry.Subtitle) && (entry.SubButtons == null || entry.SubButtons.Count == 0))
    {
        TextBox mainSub = new TextBox()
        {
            Text = entry.Subtitle,
            Left = mainBtn.Left,
            Top = top,
            Width = mainBtn.Width,
            Height = 25,
            ReadOnly = true
        };
        panel.Controls.Add(mainSub);
        top += mainSub.Height + 2;
    }
  }

    private void AdjustAllButtonPositions(Panel panel)
{
    int top = 10; // start padding
    foreach (Control ctrl in panel.Controls)
    {
        ctrl.Top = top;

        // If it's a sub-panel, auto-size to fit its children
        if (ctrl is Panel subPanel)
        {
            int subTop = 0;
            foreach (Control child in subPanel.Controls)
            {
                child.Top = subTop;
                subTop += child.Height + 2; // small spacing between sub-buttons
            }
            subPanel.Height = subTop;
        }

        top += ctrl.Height + 5; // small spacing between main buttons/panels
    }
}

private void AdjustPositionsFrom(Panel panel, Control startControl)
{
    bool startShifting = false;
    int top = 10;

    foreach (Control ctrl in panel.Controls)
    {
        if (ctrl == startControl)
        {
            startShifting = true;
            top = ctrl.Top + ctrl.Height;
            continue;
        }

        if (startShifting)
        {
            ctrl.Top = top + 2;
        }

        if (ctrl is Panel subPanel && subPanel.Visible)
        {
            top = ctrl.Top + subPanel.Height;
        }
        else
        {
            top = ctrl.Top + ctrl.Height;
        }
    }
}
    private void OpenUrl(string url)
    {
        try
        {
            if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                url = "https://" + url;
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch
        {
            MessageBox.Show("Unable to open URL: " + url);
        }
    }
    private async Task<bool> CheckForUpdates()
{
    try
    {
        using (HttpClient client = new HttpClient())
        {
            string latestVersion = (await client.GetStringAsync(LatestVersionUrl)).Trim();
            if (latestVersion != CurrentVersion)
            {
                var result = MessageBox.Show(
                    $"A newer version ({latestVersion}) is available.\n" +
                    $"You are using {CurrentVersion}.\n\n" +
                    "Do you want to download the latest version?",
                    "Update Available",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information
                );

                if (result == DialogResult.Yes)
                {
                    Process.Start(new ProcessStartInfo(GitHubReleasesUrl) { UseShellExecute = true });
                }

                return false; // not up to date → close
            }
        }
    }
    catch
    {
        
    }

    return true; // up to date → continue running
}
}
/// <summary>
/// Developer JSON maker GUI
/// </summary>
class JsonMaker : Form
{
    private TextBox nameBox, urlBox, subtitleBox;
    private CheckBox hasSubCheck;
    private Panel subPanel;
    private List<SubButtonEntry> subButtonList = new List<SubButtonEntry>();

    public JsonMaker()
    {
        this.Text = "JSON Creator";
        this.Width = 500;
        this.Height = 600;

        Label lblName = new Label() { Text = "Main Name:", Left = 10, Top = 10, Width = 100 };
        nameBox = new TextBox() { Left = 120, Top = 10, Width = 300 };
        Label lblUrl = new Label() { Text = "URL:", Left = 10, Top = 40, Width = 100 };
        urlBox = new TextBox() { Left = 120, Top = 40, Width = 300 };
        Label lblSubtitle = new Label() { Text = "Subtitle:", Left = 10, Top = 70, Width = 100 };
        subtitleBox = new TextBox() { Left = 120, Top = 70, Width = 300 };

        hasSubCheck = new CheckBox() { Text = "Has SubButtons?", Left = 10, Top = 100, Width = 200 };
        hasSubCheck.CheckedChanged += (s, e) =>
        {
            subPanel.Visible = hasSubCheck.Checked;
        };

        subPanel = new Panel() { Left = 10, Top = 130, Width = 450, Height = 300, AutoScroll = true, Visible = false };
        Button addSub = new Button() { Text = "Add SubButton", Left = 0, Top = 0, Width = 150 };
        addSub.Click += (s, e) => AddSubButtonControl();

        subPanel.Controls.Add(addSub);

        Button saveBtn = new Button() { Text = "Save JSON", Left = 10, Top = 450, Width = 100 };
        saveBtn.Click += (s, e) => SaveJson();

        this.Controls.Add(lblName);
        this.Controls.Add(nameBox);
        this.Controls.Add(lblUrl);
        this.Controls.Add(urlBox);
        this.Controls.Add(lblSubtitle);
        this.Controls.Add(subtitleBox);
        this.Controls.Add(hasSubCheck);
        this.Controls.Add(subPanel);
        this.Controls.Add(saveBtn);
    }

    private void AddSubButtonControl()
    {
        int y = subPanel.Controls.Count * 35;
        TextBox subName = new TextBox() { Left = 0, Top = y, Width = 150, PlaceholderText = "Name" };
        TextBox subUrl = new TextBox() { Left = 160, Top = y, Width = 150, PlaceholderText = "URL" };
        TextBox subTxt = new TextBox() { Left = 320, Top = y, Width = 120, PlaceholderText = "Subtitle" };

        subPanel.Controls.Add(subName);
        subPanel.Controls.Add(subUrl);
        subPanel.Controls.Add(subTxt);

        subButtonList.Add(new SubButtonEntry { Name = "", Url = "", Subtitle = "" });

        subName.TextChanged += (s, e) => subButtonList[subButtonList.Count - 1].Name = subName.Text;
        subUrl.TextChanged += (s, e) => subButtonList[subButtonList.Count - 1].Url = subUrl.Text;
        subTxt.TextChanged += (s, e) => subButtonList[subButtonList.Count - 1].Subtitle = subTxt.Text;
    }

    private void SaveJson()
    {
        UrlEntry entry = new UrlEntry
        {
            Name = nameBox.Text,
            Url = urlBox.Text,
            Subtitle = subtitleBox.Text,
            SubButtons = hasSubCheck.Checked ? subButtonList : null
        };

        string json = JsonSerializer.Serialize(new List<UrlEntry> { entry }, new JsonSerializerOptions { WriteIndented = true });

        SaveFileDialog sfd = new SaveFileDialog
        {
            Filter = "JSON Files|*.json",
            FileName = "output.json"
        };
        if (sfd.ShowDialog() == DialogResult.OK)
        {
            File.WriteAllText(sfd.FileName, json);
            MessageBox.Show("JSON saved!");
        }
    }
}


