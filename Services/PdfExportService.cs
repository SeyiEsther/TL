using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TL.Models;


namespace TL.Services
{
    public class PdfExportService
    {
        private static readonly string Red = "#CC1F2C";
        private static readonly string DarkGray = "#1a1a1a";
        private static readonly string MidGray = "#6b7280";
        private static readonly string LightGray = "#f3f4f6";
        private static readonly string BorderGray = "#e5e7eb";
        private static readonly string GreenBg = "#d1fae5";
        private static readonly string GreenText = "#065f46";
        private static readonly string AmberBg = "#fef3c7";
        private static readonly string AmberText = "#b45309";
        private static readonly string RedBg = "#fee2e2";
        private static readonly string RedText = "#991b1b";

        public byte[] GenerateShift(ShiftSubmission s)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(36);
                    page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial"));
                    page.Header().Element(ComposeHeader);
                    page.Content().Element(c => ComposeContent(c, s));
                    page.Footer().Element(ComposeFooter);
                });
            });

            return doc.GeneratePdf();
        }

        static void ComposeHeader(IContainer c)
        {
            c.Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("RITTAL").FontSize(20).Bold().FontColor("#1a1a1a");
                    col.Item().Text("Team Leader Standard Work").FontSize(11).FontColor("#6b7280");
                });
                row.ConstantItem(6).Background(Red);
            });
        }

        static void ComposeContent(IContainer c, ShiftSubmission s)
        {
            c.Column(col =>
            {
                col.Spacing(10);

                // Session
                col.Item().Border(0.5f).BorderColor("#e5e7eb").Column(inner =>
                {
                    inner.Item().Background("#f3f4f6").Padding(6)
                        .Text("SESSION DETAILS").FontSize(8).Bold().FontColor("#1a1a1a");
                    inner.Item().Padding(8).Table(t =>
                    {
                        t.ColumnsDefinition(cd => { cd.RelativeColumn(); cd.RelativeColumn(); cd.RelativeColumn(); cd.RelativeColumn(); });
                        t.Cell().Element(LabelCell).Text("Date");
                        t.Cell().Element(ValueCell).Text(s.ShiftDate.ToString("dd MMM yyyy"));
                        t.Cell().Element(LabelCell).Text("Shift");
                        t.Cell().Element(ValueCell).Text(s.Shift);
                        t.Cell().Element(LabelCell).Text("Team Leader");
                        t.Cell().Element(ValueCell).Text(s.TeamLeaderDisplay);
                        t.Cell().Element(LabelCell).Text("Hours");
                        t.Cell().Element(ValueCell).Text(s.HoursCompleted.ToString());
                    });
                });

                // Hourly checks summary
                if (s.Hours?.Any() == true)
                {
                    col.Item().Border(0.5f).BorderColor("#e5e7eb").Column(inner =>
                    {
                        inner.Item().Background("#1c2b1e").Padding(6)
                            .Text("HOURLY CHECKS").FontSize(8).Bold().FontColor("#fff");
                        foreach (var h in s.Hours.OrderBy(x => x.HourNumber))
                        {
                            inner.Item().BorderBottom(0.5f).BorderColor("#e5e7eb").Padding(6).Column(hcol =>
                            {
                                hcol.Item().Text($"Hour {h.HourNumber}").FontSize(8).Bold().FontColor("#1a1a1a");
                                hcol.Item().Table(t =>
                                {
                                    t.ColumnsDefinition(cd => { cd.RelativeColumn(3); cd.RelativeColumn(); });
                                    BoolRow(t, "Hazards observed", h.HazardsObserved);
                                    BoolRow(t, "Unsafe behaviours", h.UnsafeBehaviours);
                                    BoolRow(t, "Positive behaviours", h.PositiveBehaviours);
                                    BoolRow(t, "Quality checks completed", h.QualityChecksCompleted);
                                    BoolRow(t, "Deviations escalated", h.DeviationsEscalated);
                                    BoolRow(t, "Hourly target achieved", h.HourlyTargetAchieved);
                                    BoolRow(t, "Maintenance issues", h.MaintenanceIssues);
                                    BoolRow(t, "Materials available", h.MaterialsAvailable);
                                    BoolRow(t, "Tools available", h.ToolsAvailable);
                                    BoolRow(t, "6S completed", h.SixSCompleted);
                                    BoolRow(t, "TPM completed", h.TPMCompleted);
                                    BoolRow(t, "Wellbeing confirmed", h.WellbeingConfirmed);
                                    BoolRow(t, "Support required", h.SupportRequired);
                                    BoolRow(t, "Accidents reported", h.AccidentsReported);
                                });
                                if (!string.IsNullOrWhiteSpace(h.SafetyNotes))
                                    hcol.Item().PaddingTop(4).Text($"Safety: {h.SafetyNotes}").FontSize(8).FontColor("#6b7280");
                                if (!string.IsNullOrWhiteSpace(h.PerformanceNotes))
                                    hcol.Item().PaddingTop(2).Text($"Performance: {h.PerformanceNotes}").FontSize(8).FontColor("#6b7280");
                                if (!string.IsNullOrWhiteSpace(h.MoraleNotes))
                                    hcol.Item().PaddingTop(2).Text($"Morale: {h.MoraleNotes}").FontSize(8).FontColor("#6b7280");
                                if (h.OverallSafetyStatus != null)
                                {
                                    hcol.Item().PaddingTop(4).Row(r =>
                                    {
                                        r.AutoItem().Text("Status — ").FontSize(8);
                                        r.AutoItem().Text($"Safety: {h.OverallSafetyStatus}  Quality: {h.OverallQualityStatus}  Performance: {h.OverallPerfStatus}").FontSize(8).Bold();
                                    });
                                }
                            });
                        }
                    });
                }

                // Handover summary
                col.Item().Border(0.5f).BorderColor("#e5e7eb").Column(inner =>
                {
                    inner.Item().Background("#1a0a0a").Padding(6)
                        .Text("HANDOVER SUMMARY").FontSize(8).Bold().FontColor("#fff");
                    inner.Item().Padding(8).Column(sc =>
                    {
                        if (!string.IsNullOrWhiteSpace(s.Escalations))
                        {
                            sc.Item().Text("Escalations").FontSize(8).Bold().FontColor("#6b7280");
                            sc.Item().Background("#f3f4f6").Padding(4).Text(s.Escalations).FontSize(8);
                            sc.Item().Height(6);
                        }
                        if (!string.IsNullOrWhiteSpace(s.KeyRisks))
                        {
                            sc.Item().Text("Key risks for next shift").FontSize(8).Bold().FontColor("#6b7280");
                            sc.Item().Background("#f3f4f6").Padding(4).Text(s.KeyRisks).FontSize(8);
                            sc.Item().Height(6);
                        }
                        if (!string.IsNullOrWhiteSpace(s.Priorities))
                        {
                            sc.Item().Text("Immediate priorities").FontSize(8).Bold().FontColor("#6b7280");
                            sc.Item().Background("#f3f4f6").Padding(4).Text(s.Priorities).FontSize(8);
                        }
                    });
                });

                // Signatures
                col.Item().Border(0.5f).BorderColor("#e5e7eb").Column(inner =>
                {
                    inner.Item().Background("#f3f4f6").Padding(6)
                        .Text("SIGNATURES").FontSize(8).Bold().FontColor("#1a1a1a");
                    inner.Item().Padding(8).Table(t =>
                    {
                        t.ColumnsDefinition(cd => { cd.RelativeColumn(); cd.RelativeColumn(); });
                        t.Cell().Element(LabelCell).Text("Outgoing TL");
                        t.Cell().Element(LabelCell).Text("Incoming TL");
                        t.Cell().Element(ValueCell).Text(s.OutgoingTLSignature ?? "—");
                        t.Cell().Element(ValueCell).Text(s.IncomingTLSignature ?? "—");
                    });
                });
            });
        }

        static void ComposeFooter(IContainer c)
        {
            c.Row(row =>
            {
                row.RelativeItem().Text(t =>
                    t.Span($"Rittal TL Standard Work — {DateTime.UtcNow:dd MMM yyyy HH:mm} UTC").FontColor("#6b7280").FontSize(8));
            });
        }

        static IContainer LabelCell(IContainer c) => c.PaddingVertical(3).PaddingRight(8);
        static IContainer ValueCell(IContainer c) => c.PaddingVertical(3);

        static void BoolRow(TableDescriptor t, string label, bool? value)
        {
            t.Cell().Element(c => c.PaddingVertical(2).PaddingRight(8))
                .Text(label).FontColor("#6b7280").FontSize(8);
            t.Cell().Element(c =>
            {
                var bg = value == true ? "#d1fae5" : value == false ? "#fee2e2" : "#f3f4f6";
                var fg = value == true ? "#065f46" : value == false ? "#991b1b" : "#9ca3af";
                var lbl = value == true ? "Yes" : value == false ? "No" : "—";
                c.Background(bg).Padding(2).AlignCenter()
                    .Text(lbl).FontColor(fg).Bold().FontSize(8);
            });
        }
    }
}