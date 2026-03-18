using System.Diagnostics;
using SecurityProgram.App.Core.Encryption;
using SecurityProgram.App.Core.Reporting;
using SecurityProgram.App.ViewModels;

internal static class Program
{
    private static readonly List<string> Passes = new();
    private static readonly List<string> Failures = new();
    private static string _artifactDir = string.Empty;

    [STAThread]
    private static async Task<int> Main()
    {
        _artifactDir = Path.Combine(Directory.GetCurrentDirectory(), "artifacts", "regression");
        Directory.CreateDirectory(_artifactDir);

        await TestEncryptionRoundTripAsync();
        TestPasswordAnalyzer();
        await TestNetworkScanAsync();
        TestEventLogMonitorInitialization();
        await TestReportPreviewAndPdfExportAsync();

        Console.WriteLine();
        Console.WriteLine("==== Regression Smoke Summary ====");

        foreach (var pass in Passes)
        {
            Console.WriteLine($"PASS: {pass}");
        }

        foreach (var failure in Failures)
        {
            Console.WriteLine($"FAIL: {failure}");
        }

        Console.WriteLine($"Result: PASS={Passes.Count}, FAIL={Failures.Count}");
        return Failures.Count == 0 ? 0 : 1;
    }

    private static async Task TestEncryptionRoundTripAsync()
    {
        const string caseName = "File Encryption round-trip";
        try
        {
            var sourcePath = Path.Combine(_artifactDir, "encryption_input.txt");
            var originalContent = $"security-regression-{DateTime.UtcNow:O}";
            File.WriteAllText(sourcePath, originalContent);

            var service = new AesFileCryptoService();
            var encrypted = service.EncryptFile(sourcePath, "Str0ng!Passw0rd#2026");
            var decrypted = service.DecryptFile(encrypted, "Str0ng!Passw0rd#2026");

            var decryptedContent = File.ReadAllText(decrypted);
            var ok = File.Exists(encrypted)
                     && File.Exists(decrypted)
                     && string.Equals(originalContent, decryptedContent, StringComparison.Ordinal);

            Record(ok, caseName, ok
                ? $"encrypted={Path.GetFileName(encrypted)}, decrypted={Path.GetFileName(decrypted)}"
                : "decrypted content mismatch");
        }
        catch (Exception ex)
        {
            Record(false, caseName, ex.Message);
        }

        await Task.CompletedTask;
    }

    private static void TestPasswordAnalyzer()
    {
        const string caseName = "Password Check scoring";
        try
        {
            var vm = new PasswordViewModel();

            vm.PasswordInput = "abc";
            var weakOk = vm.PasswordScore < 60;

            vm.PasswordInput = "Abcd!1234Efgh";
            var strongOk = vm.PasswordScore >= 60;

            vm.GenerateSamplePasswordCommand.Execute(null);
            var generatedOk = vm.PasswordInput.Length >= 12;

            var ok = weakOk && strongOk && generatedOk && vm.RuleChecklist.Count >= 6;
            Record(ok, caseName, ok
                ? $"weak={weakOk}, strong={strongOk}, generatedLength={vm.PasswordInput.Length}"
                : $"weak={weakOk}, strong={strongOk}, generated={generatedOk}, rules={vm.RuleChecklist.Count}");
        }
        catch (Exception ex)
        {
            Record(false, caseName, ex.Message);
        }
    }

    private static async Task TestNetworkScanAsync()
    {
        const string caseName = "Network scan refresh";
        try
        {
            var vm = new NetworkViewModel();
            var initialSummary = vm.ScanSummary;

            var timeout = TimeSpan.FromSeconds(20);
            var sw = Stopwatch.StartNew();

            while (sw.Elapsed < timeout)
            {
                var done = !vm.IsScanning && !string.Equals(vm.ScanSummary, initialSummary, StringComparison.Ordinal);
                if (done)
                {
                    break;
                }

                await Task.Delay(250);
            }

            var summaryChanged = !string.Equals(vm.ScanSummary, initialSummary, StringComparison.Ordinal);
            var statusKnown = vm.ScanSummary.StartsWith("Last scan:", StringComparison.Ordinal)
                              || vm.ScanSummary.StartsWith("Network scan failed:", StringComparison.Ordinal);

            var ok = summaryChanged && statusKnown;
            Record(ok, caseName, ok
                ? vm.ScanSummary
                : $"summary='{vm.ScanSummary}'");
        }
        catch (Exception ex)
        {
            Record(false, caseName, ex.Message);
        }
    }

    private static void TestEventLogMonitorInitialization()
    {
        const string caseName = "Event Log monitor init";
        try
        {
            var vm = new EventLogViewModel();
            var ok = !string.IsNullOrWhiteSpace(vm.FilterInfo);

            Record(ok, caseName, ok ? vm.FilterInfo : "FilterInfo is empty");
        }
        catch (Exception ex)
        {
            Record(false, caseName, ex.Message);
        }
    }

    private static async Task TestReportPreviewAndPdfExportAsync()
    {
        const string caseName = "Report preview + PDF export";
        try
        {
            var passwordVm = new PasswordViewModel
            {
                PasswordInput = "Abcd!1234Efgh"
            };

            var networkVm = new NetworkViewModel();
            var eventVm = new EventLogViewModel();

            var timeout = TimeSpan.FromSeconds(20);
            var sw = Stopwatch.StartNew();
            while (networkVm.IsScanning && sw.Elapsed < timeout)
            {
                await Task.Delay(250);
            }

            var reportVm = new ReportViewModel(passwordVm, networkVm, eventVm);
            reportVm.GeneratePreviewCommand.Execute(null);

            var hasPreview = !string.IsNullOrWhiteSpace(reportVm.ReportPreview)
                             && reportVm.ReportPreview.Contains("Password Analysis", StringComparison.Ordinal);

            var pdfPath = Path.Combine(_artifactDir, $"SecurityReport_Regression_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
            var pdfService = new PdfReportService();
            var savedPath = pdfService.ExportReport(
                pdfPath,
                reportVm.ReportTitle,
                reportVm.AnalystName,
                reportVm.TargetSystem,
                DateTime.Now,
                reportVm.ReportPreview);

            var pdfExists = File.Exists(savedPath) && new FileInfo(savedPath).Length > 0;
            var ok = hasPreview && pdfExists;

            Record(ok, caseName, ok
                ? $"preview=ok, pdf={savedPath}"
                : $"preview={hasPreview}, pdfExists={pdfExists}");
        }
        catch (Exception ex)
        {
            Record(false, caseName, ex.Message);
        }
    }

    private static void Record(bool passed, string caseName, string detail)
    {
        var entry = $"{caseName} | {detail}";
        if (passed)
        {
            Passes.Add(entry);
        }
        else
        {
            Failures.Add(entry);
        }
    }
}
