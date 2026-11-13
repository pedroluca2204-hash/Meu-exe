using System.Diagnostics;
using System.Runtime.InteropServices;

namespace JMCompetitiveOptimizer;

public sealed class MainForm : Form
{
    private readonly Color bg = Color.FromArgb(13, 17, 25);
    private readonly Color panel = Color.FromArgb(23, 30, 42);
    private readonly Color blue = Color.FromArgb(50, 155, 255);
    private readonly Label cpuValue = new();
    private readonly Label ramValue = new();
    private readonly Label diskValue = new();
    private readonly Label gpuValue = new();
    private readonly Label status = new();
    private readonly System.Windows.Forms.Timer timer = new() { Interval = 1200 };
    private readonly DateTime started = DateTime.Now;
    private ulong previousIdle, previousKernel, previousUser;

    public MainForm()
    {
        Text = "JM COMPETITIVE OPTIMIZER";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(1050, 690);
        MinimumSize = new Size(900, 620);
        BackColor = bg;
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 9.5f);
        BuildUi();
        timer.Tick += (_, _) => RefreshStats();
        timer.Start();
        RefreshStats();
    }

    private void BuildUi()
    {
        var sidebar = new Panel { Dock = DockStyle.Left, Width = 220, BackColor = panel };
        Controls.Add(sidebar);
        var brand = new Label {
            Text = "JM COMPETITIVE\nOPTIMIZER",
            Font = new Font("Segoe UI", 17, FontStyle.Bold),
            ForeColor = blue, AutoSize = false, Location = new Point(22, 28),
            Size = new Size(185, 70)
        };
        sidebar.Controls.Add(brand);
        var nav = new[] { "Painel", "Monitor", "Diagnóstico", "Game Booster", "Meu hardware" };
        int y = 135;
        foreach (var item in nav)
        {
            var b = new Label {
                Text = "   " + item, Location = new Point(14, y),
                Size = new Size(190, 42), TextAlign = ContentAlignment.MiddleLeft,
                BackColor = item == "Painel" ? Color.FromArgb(35, 55, 78) : panel,
                ForeColor = Color.White, Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            sidebar.Controls.Add(b);
            y += 50;
        }
        var version = new Label { Text = "VERSÃO 3.0  •  BETA", ForeColor = Color.Gray,
            Location = new Point(22, 625), AutoSize = true };
        sidebar.Controls.Add(version);

        var header = new Label { Text = "Visão geral do sistema", Font = new Font("Segoe UI", 23, FontStyle.Bold),
            ForeColor = Color.White, Location = new Point(255, 28), AutoSize = true };
        Controls.Add(header);
        var subtitle = new Label { Text = "Monitoramento local do computador • atualização automática",
            ForeColor = Color.Silver, Location = new Point(258, 72), AutoSize = true };
        Controls.Add(subtitle);

        AddMetric("PROCESSADOR", cpuValue, 255, 125);
        AddMetric("MEMÓRIA RAM", ramValue, 620, 125);
        AddMetric("ARMAZENAMENTO", diskValue, 255, 285);
        AddMetric("PLACA DE VÍDEO", gpuValue, 620, 285);

        var diag = MakeCard(255, 445, 725, 165);
        Controls.Add(diag);
        var diagTitle = new Label { Text = "Diagnóstico rápido", Font = new Font("Segoe UI", 14, FontStyle.Bold),
            ForeColor = blue, Location = new Point(20, 16), AutoSize = true };
        diag.Controls.Add(diagTitle);
        status.Text = "Coletando informações do Windows...";
        status.ForeColor = Color.Gainsboro;
        status.Location = new Point(20, 55);
        status.Size = new Size(680, 85);
        diag.Controls.Add(status);

        var note = new Label {
            Text = "Este aplicativo apenas monitora informações. Nenhuma configuração do Windows é alterada automaticamente.",
            ForeColor = Color.Gray, Location = new Point(258, 625), AutoSize = true
        };
        Controls.Add(note);
    }

    private void AddMetric(string title, Label value, int x, int y)
    {
        var card = MakeCard(x, y, 340, 135);
        Controls.Add(card);
        var t = new Label { Text = title, ForeColor = Color.Silver, Font = new Font("Segoe UI", 9, FontStyle.Bold),
            Location = new Point(18, 16), AutoSize = true };
        card.Controls.Add(t);
        value.Text = "--";
        value.ForeColor = Color.White;
        value.Font = new Font("Segoe UI", 24, FontStyle.Bold);
        value.Location = new Point(18, 48);
        value.Size = new Size(300, 55);
        card.Controls.Add(value);
    }

    private Panel MakeCard(int x, int y, int w, int h) =>
        new() { Location = new Point(x, y), Size = new Size(w, h), BackColor = panel, BorderStyle = BorderStyle.FixedSingle };

    private void RefreshStats()
    {
        try
        {
            if (GetSystemTimes(out var idle, out var kernel, out var user))
            {
                ulong i = ToUInt64(idle), k = ToUInt64(kernel), u = ToUInt64(user);
                if (previousKernel != 0)
                {
                    ulong total = (k - previousKernel) + (u - previousUser);
                    ulong busy = total > (i - previousIdle) ? total - (i - previousIdle) : 0;
                    double pct = total == 0 ? 0 : busy * 100.0 / total;
                    cpuValue.Text = $"{Math.Clamp(pct, 0, 100):0}% de uso";
                }
                previousIdle = i; previousKernel = k; previousUser = u;
            }
        }
        catch { cpuValue.Text = "Indisponível"; }

        try
        {
            MEMORYSTATUSEX mem = new() { dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>() };
            if (GlobalMemoryStatusEx(ref mem))
            {
                double total = mem.ullTotalPhys / 1073741824.0;
                double used = (mem.ullTotalPhys - mem.ullAvailPhys) / 1073741824.0;
                ramValue.Text = $"{used:0.0} / {total:0.0} GB";
            }
        }
        catch { ramValue.Text = "Indisponível"; }

        try
        {
            var drive = DriveInfo.GetDrives().FirstOrDefault(d => d.IsReady && d.Name.StartsWith(Path.GetPathRoot(Environment.SystemDirectory)!, StringComparison.OrdinalIgnoreCase));
            diskValue.Text = drive is null ? "Não detectado" : $"{drive.AvailableFreeSpace / 1073741824.0:0} GB livres";
        }
        catch { diskValue.Text = "Indisponível"; }

        gpuValue.Text = "Leitura não disponível";
        status.Text = $"Windows detectado: {Environment.OSVersion.Version}   •   Arquitetura: {RuntimeInformation.OSArchitecture}\r\n" +
                      $"Tempo aberto: {(DateTime.Now - started):hh\\:mm\\:ss}\r\n" +
                      "Dica: feche programas que não estiver usando. Este diagnóstico é informativo e não aplica ajustes.";
    }

    private static ulong ToUInt64(FILETIME t) => ((ulong)t.dwHighDateTime << 32) | t.dwLowDateTime;

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetSystemTimes(out FILETIME lpIdleTime, out FILETIME lpKernelTime, out FILETIME lpUserTime);

    [StructLayout(LayoutKind.Sequential)]
    private struct FILETIME { public uint dwLowDateTime; public uint dwHighDateTime; }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MEMORYSTATUSEX
    {
        public uint dwLength, dwMemoryLoad;
        public ulong ullTotalPhys, ullAvailPhys, ullTotalPageFile, ullAvailPageFile, ullTotalVirtual, ullAvailVirtual, ullAvailExtendedVirtual;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        timer.Stop();
        timer.Dispose();
        base.OnFormClosed(e);
    }
}
