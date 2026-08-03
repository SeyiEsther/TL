using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TL.Models;


namespace TL.Services
{
    public class PdfExportService
    {
        private readonly DocumentNumberService? _docs;
        // Optional so tests can construct the service without a database; DI
        // always supplies it in the running app.
        public PdfExportService(DocumentNumberService? docs = null) => _docs = docs;

        private string DocNo(string formType) => _docs?.Get(formType) ?? "";

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

        public byte[] GenerateAudit(AuditSubmission a)
        {
            QuestPDF.Settings.License = LicenseType.Community;
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(36);
                    page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial"));
                    page.Header().Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("RITTAL").FontSize(20).Bold().FontColor(DarkGray);
                                c.Item().Text("Senior Management Walkaround Audit").FontSize(11).FontColor(MidGray);
                            });
                            row.ConstantItem(6).Background(Red);
                        });
                        col.Item().Height(10);
                    });
                    page.Content().Column(col =>
                    {
                        col.Spacing(8);

                        col.Item().Border(0.5f).BorderColor(BorderGray).Column(inner =>
                        {
                            inner.Item().Background(LightGray).Padding(6)
                                .Text("AUDIT DETAILS").FontSize(8).Bold().FontColor(DarkGray);
                            inner.Item().Padding(8).Table(t =>
                            {
                                t.ColumnsDefinition(cd => { cd.RelativeColumn(); cd.RelativeColumn(); cd.RelativeColumn(); cd.RelativeColumn(); });
                                t.Cell().Element(LabelCell).Text("Date");
                                t.Cell().Element(ValueCell).Text(a.AuditDate.ToString("dd MMM yyyy"));
                                t.Cell().Element(LabelCell).Text("Area");
                                t.Cell().Element(ValueCell).Text(a.Area);
                                t.Cell().Element(LabelCell).Text("Auditor");
                                t.Cell().Element(ValueCell).Text(a.AuditorName);
                                t.Cell().Element(LabelCell).Text("TL on shift");
                                t.Cell().Element(ValueCell).Text(a.TLOnShift ?? "—");
                                t.Cell().Element(LabelCell).Text("Shift observed");
                                t.Cell().Element(ValueCell).Text(a.ShiftObserved ?? "—");
                                t.Cell().Element(LabelCell).Text("Submitted");
                                t.Cell().Element(ValueCell).Text(a.SubmittedAt.ToLocalTime().ToString("dd MMM yyyy HH:mm"));
                            });
                        });

                        col.Item().Border(0.5f).BorderColor(BorderGray).Column(inner =>
                        {
                            inner.Item().Background("#14532d").Padding(6)
                                .Text("HEALTH & SAFETY").FontSize(8).Bold().FontColor("#fff");
                            inner.Item().Padding(8).Table(t =>
                            {
                                t.ColumnsDefinition(cd => { cd.RelativeColumn(3); cd.RelativeColumn(); });
                                BoolRow(t, "Hazards observed", a.HazardsObserved);
                                BoolRow(t, "Unsafe behaviours observed", a.UnsafeBehavioursObserved);
                                BoolRow(t, "Positive behaviours praised", a.PositiveBehavioursPraised);
                            });
                            if (!string.IsNullOrWhiteSpace(a.SafetyNotes))
                                inner.Item().PaddingHorizontal(8).PaddingBottom(8)
                                    .Background(LightGray).Padding(4).Text(a.SafetyNotes).FontSize(8);
                        });

                        col.Item().Border(0.5f).BorderColor(BorderGray).Column(inner =>
                        {
                            inner.Item().Background("#1e3a5f").Padding(6)
                                .Text("QUALITY").FontSize(8).Bold().FontColor("#fff");
                            inner.Item().Padding(8).Table(t =>
                            {
                                t.ColumnsDefinition(cd => { cd.RelativeColumn(3); cd.RelativeColumn(); });
                                BoolRow(t, "Quality checks completed", a.QualityChecksCompleted);
                                BoolRow(t, "Deviations escalated", a.DeviationsEscalated);
                                BoolRow(t, "Non-compliance addressed", a.NonComplianceAddressed);
                            });
                            if (!string.IsNullOrWhiteSpace(a.QualityNotes))
                                inner.Item().PaddingHorizontal(8).PaddingBottom(8)
                                    .Background(LightGray).Padding(4).Text(a.QualityNotes).FontSize(8);
                        });

                        col.Item().Border(0.5f).BorderColor(BorderGray).Column(inner =>
                        {
                            inner.Item().Background("#1e1b4b").Padding(6)
                                .Text("PERFORMANCE").FontSize(8).Bold().FontColor("#fff");
                            inner.Item().Padding(8).Table(t =>
                            {
                                t.ColumnsDefinition(cd => { cd.RelativeColumn(3); cd.RelativeColumn(); });
                                BoolRow(t, "Hourly target achieved", a.HourlyTargetAchieved);
                                BoolRow(t, "Maintenance issues", a.MaintenanceIssues);
                                BoolRow(t, "Materials available", a.MaterialsAvailable);
                                BoolRow(t, "Tools available", a.ToolsAvailable);
                                BoolRow(t, "Escalations needed", a.EscalationsNeeded);
                                BoolRow(t, "Parts confirmed", a.PartsConfirmed);
                                BoolRow(t, "Parts ID correct", a.PartsIdCorrect);
                                BoolRow(t, "NC parts stored correctly", a.NCPartsStoredCorrectly);
                                BoolRow(t, "6S completed", a.SixSCompleted);
                                BoolRow(t, "TPM completed", a.TPMCompleted);
                            });
                            if (!string.IsNullOrWhiteSpace(a.PerformanceNotes))
                                inner.Item().PaddingHorizontal(8).PaddingBottom(8)
                                    .Background(LightGray).Padding(4).Text(a.PerformanceNotes).FontSize(8);
                        });

                        col.Item().Border(0.5f).BorderColor(BorderGray).Column(inner =>
                        {
                            inner.Item().Background("#4a1d1d").Padding(6)
                                .Text("MORALE & WELLBEING").FontSize(8).Bold().FontColor("#fff");
                            inner.Item().Padding(8).Table(t =>
                            {
                                t.ColumnsDefinition(cd => { cd.RelativeColumn(3); cd.RelativeColumn(); });
                                BoolRow(t, "Wellbeing confirmed", a.WellbeingConfirmed);
                                BoolRow(t, "Support required", a.SupportRequired);
                            });
                            if (!string.IsNullOrWhiteSpace(a.MoraleNotes))
                                inner.Item().PaddingHorizontal(8).PaddingBottom(8)
                                    .Background(LightGray).Padding(4).Text(a.MoraleNotes).FontSize(8);
                        });

                        col.Item().Border(0.5f).BorderColor(BorderGray).Column(inner =>
                        {
                            inner.Item().Background(DarkGray).Padding(6)
                                .Text("AUDIT VERDICT").FontSize(8).Bold().FontColor("#fff");
                            inner.Item().Padding(8).Table(t =>
                            {
                                t.ColumnsDefinition(cd => { cd.RelativeColumn(3); cd.RelativeColumn(); });
                                BoolRow(t, "Accidents / near-misses observed", a.AccidentsObserved);
                                StatusRow(t, "Overall Safety status", a.OverallSafetyStatus);
                                StatusRow(t, "Overall Quality status", a.OverallQualityStatus);
                                StatusRow(t, "Overall Performance status", a.OverallPerfStatus);
                            });
                        });

                        col.Item().Border(0.5f).BorderColor(BorderGray).Column(inner =>
                        {
                            inner.Item().Background("#78350f").Padding(6)
                                .Text("FINDINGS & ACTIONS").FontSize(8).Bold().FontColor("#fff");
                            inner.Item().Padding(8).Column(sc =>
                            {
                                void NoteBlock(string label, string? text, string bg)
                                {
                                    if (!string.IsNullOrWhiteSpace(text))
                                    {
                                        sc.Item().Text(label).FontSize(8).Bold().FontColor(MidGray);
                                        sc.Item().Background(bg).Padding(4).Text(text).FontSize(8);
                                        sc.Item().Height(6);
                                    }
                                }
                                NoteBlock("Actions raised", a.ActionsRaised, LightGray);
                                NoteBlock("Good practice observed", a.GoodPracticeObserved, GreenBg);
                                NoteBlock("Follow-up required", a.FollowUpRequired, AmberBg);
                                if (!string.IsNullOrWhiteSpace(a.ActionOwner) || a.ActionDueDate.HasValue)
                                {
                                    sc.Item().Table(t =>
                                    {
                                        t.ColumnsDefinition(cd => { cd.RelativeColumn(); cd.RelativeColumn(); });
                                        t.Cell().Element(LabelCell).Text("Action owner");
                                        t.Cell().Element(LabelCell).Text("Due date");
                                        t.Cell().Element(ValueCell).Text(a.ActionOwner ?? "—");
                                        t.Cell().Element(ValueCell).Text(a.ActionDueDate?.ToString("dd MMM yyyy") ?? "—");
                                    });
                                }
                            });
                        });

                        col.Item().Border(0.5f).BorderColor(BorderGray).Column(inner =>
                        {
                            inner.Item().Background(LightGray).Padding(6)
                                .Text("SIGN-OFF").FontSize(8).Bold().FontColor(DarkGray);
                            inner.Item().Padding(8).Table(t =>
                            {
                                t.ColumnsDefinition(cd => { cd.RelativeColumn(); cd.RelativeColumn(); });
                                t.Cell().Element(LabelCell).Text("Auditor signature");
                                t.Cell().Element(LabelCell).Text("Date");
                                t.Cell().Element(ValueCell).Text(a.AuditorSignature ?? "—");
                                t.Cell().Element(ValueCell).Text(a.AuditDate.ToString("dd MMM yyyy"));
                            });
                        });
                    });
                    page.Footer().Row(row =>
                    {
                        row.RelativeItem().Text(t =>
                            t.Span($"Production Audit System — {DateTime.UtcNow:dd MMM yyyy HH:mm} UTC").FontColor(MidGray).FontSize(8));
                    });
                });
            }).GeneratePdf();
        }

        static void StatusRow(TableDescriptor t, string label, string? value)
        {
            t.Cell().Element(c => c.PaddingVertical(2).PaddingRight(8))
                .Text(label).FontColor(MidGray).FontSize(8);
            t.Cell().Element(c =>
            {
                var bg = value == "Green" ? GreenBg : value == "Amber" ? AmberBg : value == "Red" ? RedBg : LightGray;
                var fg = value == "Green" ? GreenText : value == "Amber" ? AmberText : value == "Red" ? RedText : MidGray;
                c.Background(bg).Padding(2).AlignCenter()
                    .Text(value ?? "—").FontColor(fg).Bold().FontSize(8);
            });
        }

        public byte[] GenerateHodDaily(HodDailyAudit a, List<HodAuditAnswer> answers, List<HodEffectivenessFinding>? effectiveness = null)
        {
            QuestPDF.Settings.License = LicenseType.Community;
            _ = effectiveness;
            var band = HodAuditScoring.RatingBand(a.TotalScore, a.MaxScore);
            var bandColor = HodAuditScoring.BandColor(a.TotalScore, a.MaxScore);
            var sections = answers.GroupBy(x => x.Section ?? "Other").ToList();
            // Section header colours cycle through a palette
            string[] SectionColors = ["#1e3a5f", "#14532d", "#1e1b4b", "#78350f", "#1a1a2e", "#1e3a5f"];

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.MarginHorizontal(36);
                    page.MarginTop(28);
                    page.MarginBottom(28);
                    page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial"));

                    // ── Header ──────────────────────────────────────────────
                    page.Header().BorderBottom(2).BorderColor(Red).PaddingBottom(10).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("RITTAL CSM Plymouth").FontSize(18).Bold().FontColor(DarkGray);
                            c.Item().Text($"HoD Audit — {HodAuditTypes.LabelFor(a.AuditType)}").FontSize(10).FontColor(MidGray);
                        });
                        // Score badge top-right
                        row.ConstantItem(72).AlignRight().AlignMiddle().Column(c =>
                        {
                            c.Item().Background(bandColor).Padding(6).AlignCenter().Column(inner =>
                            {
                                inner.Item().Text($"{a.TotalScore}/{a.MaxScore}").FontSize(18).Bold().FontColor("#fff");
                                inner.Item().Text(band.ToUpperInvariant()).FontSize(6).Bold().FontColor("#fff");
                            });
                        });
                    });

                    // ── Content ─────────────────────────────────────────────
                    page.Content().PaddingTop(12).Column(col =>
                    {
                        col.Spacing(6);

                        // Audit details strip
                        col.Item().Background(LightGray).Border(0.5f).BorderColor(BorderGray)
                            .Padding(10).Table(t =>
                        {
                            t.ColumnsDefinition(cd =>
                            {
                                cd.ConstantColumn(70); cd.RelativeColumn();
                                cd.ConstantColumn(70); cd.RelativeColumn();
                            });
                            void MetaCell(string label, string value)
                            {
                                t.Cell().PaddingVertical(2).Text(label).FontSize(8).FontColor(MidGray).Bold();
                                t.Cell().PaddingVertical(2).Text(value).FontSize(8).FontColor(DarkGray);
                            }
                            MetaCell("Date", a.AuditDate.ToString("dd MMM yyyy"));
                            MetaCell("Area", a.Department);
                            MetaCell("Auditor", a.AuditorName);
                            MetaCell("Submitted", a.SubmittedAt.ToLocalTime().ToString("dd MMM yyyy HH:mm"));
                        });

                        // Question sections
                        var colorIdx = 0;
                        foreach (var sec in sections)
                        {
                            var hdrColor = SectionColors[colorIdx++ % SectionColors.Length];
                            col.Item().Border(0.5f).BorderColor(BorderGray).Column(inner =>
                            {
                                inner.Item().Background(hdrColor).PaddingHorizontal(10).PaddingVertical(7)
                                    .Text(sec.Key.ToUpperInvariant()).FontSize(8).Bold().FontColor("#fff");

                                inner.Item().Table(t =>
                                {
                                    t.ColumnsDefinition(cd =>
                                    {
                                        cd.RelativeColumn(5);   // question
                                        cd.ConstantColumn(60);  // pass/fail badge
                                        cd.RelativeColumn(3);   // evidence
                                    });

                                    // Column headers
                                    t.Cell().Background("#f9fafb").PaddingHorizontal(10).PaddingVertical(4)
                                        .Text("Question").FontSize(7).Bold().FontColor(MidGray);
                                    t.Cell().Background("#f9fafb").AlignCenter().PaddingVertical(4)
                                        .Text("Result").FontSize(7).Bold().FontColor(MidGray);
                                    t.Cell().Background("#f9fafb").PaddingHorizontal(10).PaddingVertical(4)
                                        .Text("Evidence / Notes").FontSize(7).Bold().FontColor(MidGray);

                                    foreach (var q in sec)
                                    {
                                        var rowBg = q.Pass == false ? "#fff5f5" : "#fff";
                                        t.Cell().Background(rowBg).BorderTop(0.5f).BorderColor(BorderGray)
                                            .PaddingHorizontal(10).PaddingVertical(5)
                                            .Text(q.Label ?? "—").FontSize(8).FontColor(DarkGray);
                                        PassFailRow(t, q.Pass, rowBg);
                                        t.Cell().Background(rowBg).BorderTop(0.5f).BorderColor(BorderGray)
                                            .PaddingHorizontal(10).PaddingVertical(5)
                                            .Text(q.Evidence ?? "—").FontSize(8).FontColor(MidGray).Italic();
                                    }
                                });
                            });
                        }

                        // Findings
                        if (!string.IsNullOrWhiteSpace(a.ActionsRaised) || !string.IsNullOrWhiteSpace(a.GoodPractice))
                        {
                            col.Item().Border(0.5f).BorderColor(BorderGray).Column(inner =>
                            {
                                inner.Item().Background("#78350f").PaddingHorizontal(10).PaddingVertical(7)
                                    .Text("FINDINGS & ACTIONS").FontSize(8).Bold().FontColor("#fff");
                                inner.Item().Padding(12).Column(fc =>
                                {
                                    fc.Spacing(10);
                                    void FindingBlock(string label, string? text, string bg)
                                    {
                                        if (string.IsNullOrWhiteSpace(text)) return;
                                        fc.Item().Text(label).FontSize(8).Bold().FontColor(MidGray);
                                        var lines = text.Replace("\r\n", "\n").Split('\n')
                                            .Select(l => l.Trim()).Where(l => l.Length > 0).ToList();
                                        fc.Item().Background(bg).Border(0.5f).BorderColor(BorderGray)
                                            .Padding(10).Column(lc =>
                                        {
                                            lc.Spacing(4);
                                            foreach (var line in lines)
                                                lc.Item().Text(t =>
                                                {
                                                    if (lines.Count > 1) t.Span("• ").FontColor(MidGray);
                                                    t.Span(line).FontSize(9).FontColor(DarkGray);
                                                });
                                        });
                                    }
                                    FindingBlock("Actions raised", a.ActionsRaised, "#fff7ed");
                                    FindingBlock("Good practice observed", a.GoodPractice, GreenBg);
                                });
                            });
                        }

                        // Sign-off
                        col.Item().Border(0.5f).BorderColor(BorderGray).Column(inner =>
                        {
                            inner.Item().Background(LightGray).PaddingHorizontal(10).PaddingVertical(7)
                                .Text("SIGN-OFF").FontSize(8).Bold().FontColor(DarkGray);
                            inner.Item().Padding(12).Row(row =>
                            {
                                void SigBox(string label, string? value)
                                {
                                    row.RelativeItem().PaddingRight(8).Border(1).BorderColor(BorderGray)
                                        .Background("#fff").MinHeight(56).Padding(10).Column(sc =>
                                    {
                                        sc.Item().Text(label).FontSize(8).FontColor(MidGray);
                                        sc.Item().Height(6);
                                        sc.Item().Text(string.IsNullOrWhiteSpace(value) ? "—" : value)
                                            .FontSize(13).Bold().FontColor(DarkGray);
                                    });
                                }
                                SigBox("Auditor signature", a.AuditorSignature);
                                SigBox("TL signature", a.TeamLeaderSignature);
                            });
                        });
                    });

                    // ── Footer ───────────────────────────────────────────────
                    page.Footer().PaddingTop(6).BorderTop(0.5f).BorderColor(BorderGray).Row(row =>
                    {
                        row.RelativeItem().Text($"Production Audit System — {DateTime.UtcNow:dd MMM yyyy HH:mm} UTC")
                            .FontSize(7).FontColor(MidGray);
                        row.AutoItem().Text(txt =>
                        {
                            txt.Span("Page ").FontSize(7).FontColor(MidGray);
                            txt.CurrentPageNumber().FontSize(7).FontColor(MidGray);
                            txt.Span(" of ").FontSize(7).FontColor(MidGray);
                            txt.TotalPages().FontSize(7).FontColor(MidGray);
                        });
                    });
                });
            }).GeneratePdf();
        }

        static void HodScoreFooter(IContainer container, int total, int max, string band)
        {
            var color = HodAuditScoring.BandColor(total, max);
            container.Border(1.5f).BorderColor(DarkGray).Background(LightGray).Padding(24).AlignCenter().Column(col =>
            {
                col.Item().Text("AUDIT SCORE").FontSize(11).Bold().FontColor(MidGray);
                col.Item().Height(10);
                col.Item().Text(max > 0 ? $"{total} / {max}" : "—").FontSize(42).Bold().FontColor(color);
                if (max > 0)
                {
                    var pct = total * 100 / max;
                    col.Item().Height(6);
                    col.Item().Text($"{pct}%").FontSize(28).Bold().FontColor(color);
                }
                col.Item().Height(8);
                col.Item().Text(band.ToUpperInvariant()).FontSize(22).Bold().FontColor(DarkGray);
            });
        }

        static void PassFailRow(TableDescriptor t, bool? pass, string? rowBg = null)
        {
            t.Cell().Element(c =>
            {
                var bg = pass == true ? GreenBg : pass == false ? RedBg : LightGray;
                var fg = pass == true ? GreenText : pass == false ? RedText : MidGray;
                var lbl = pass == true ? "Pass (1)" : pass == false ? "Fail (0)" : "—";
                var cell = rowBg != null
                    ? c.Background(rowBg).BorderTop(0.5f).BorderColor(BorderGray).AlignCenter().AlignMiddle().PaddingVertical(5)
                    : c.AlignCenter().AlignMiddle().Padding(2);
                cell.Element(inner => inner.Background(bg).Padding(3).AlignCenter()
                    .Text(lbl).FontColor(fg).Bold().FontSize(8));
            });
        }

        public byte[] GenerateSeniorWeekly(SeniorWeeklyAudit a)
        {
            QuestPDF.Settings.License = LicenseType.Community;
            var overall = SeniorAuditScoring.OverallScore(a);

            void Section(ColumnDescriptor col, string title, string headerColor,
                (string Label, byte? Score)[] rows, string? notes)
            {
                col.Item().Border(0.5f).BorderColor(BorderGray).Column(inner =>
                {
                    inner.Item().Background(headerColor).Padding(6)
                        .Text(title).FontSize(8).Bold().FontColor("#fff");
                    inner.Item().Padding(8).Table(t =>
                    {
                        t.ColumnsDefinition(cd => { cd.RelativeColumn(4); cd.RelativeColumn(); });
                        foreach (var row in rows)
                        {
                            t.Cell().PaddingVertical(2).Text(row.Label).FontColor(MidGray).FontSize(8);
                            ScoreRow(t, row.Score);
                        }
                    });
                    if (!string.IsNullOrWhiteSpace(notes))
                        inner.Item().PaddingHorizontal(8).PaddingBottom(8)
                            .Background(LightGray).Padding(4).Text(notes).FontSize(8);
                });
            }

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(36);
                    page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial"));
                    page.Header().Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("RITTAL CSM Plymouth").FontSize(20).Bold().FontColor(DarkGray);
                                c.Item().Text("Senior Team Weekly Audit").FontSize(11).FontColor(MidGray);
                            });
                            row.ConstantItem(6).Background(Red);
                        });
                        col.Item().Height(10);
                    });
                    page.Content().Column(col =>
                    {
                        col.Spacing(8);
                        col.Item().Border(0.5f).BorderColor(BorderGray).Column(inner =>
                        {
                            inner.Item().Background(LightGray).Padding(6).Text("AUDIT DETAILS").FontSize(8).Bold();
                            inner.Item().Padding(8).Table(t =>
                            {
                                t.ColumnsDefinition(cd => { cd.RelativeColumn(); cd.RelativeColumn(); cd.RelativeColumn(); cd.RelativeColumn(); });
                                t.Cell().Element(LabelCell).Text("Date");
                                t.Cell().Element(ValueCell).Text(a.AuditDate.ToString("dd MMM yyyy"));
                                t.Cell().Element(LabelCell).Text("Area");
                                t.Cell().Element(ValueCell).Text(a.Area);
                                t.Cell().Element(LabelCell).Text("Auditor");
                                t.Cell().Element(ValueCell).Text(a.AuditorName);
                                t.Cell().Element(LabelCell).Text("Submitted");
                                t.Cell().Element(ValueCell).Text(a.SubmittedAt.ToLocalTime().ToString("dd MMM yyyy HH:mm"));
                            });
                        });

                        Section(col, "LEADERSHIP & GOVERNANCE", "#1e3a5f",
                        [
                            ("Shift handover standards being followed?", a.HandoverStandardsFollowed),
                            ("RPS boards up to date?", a.VisualManagementCurrent),
                            ("Escalation paths being used correctly?", a.EscalationPathsUsed),
                        ], a.GovernanceNotes);

                        Section(col, "SAFETY CULTURE", "#14532d",
                        [
                            ("PPE compliance 100% across the area?", a.PpeComplianceFull),
                            ("Near-misses being reported?", a.NearMissesReported),
                        ], a.SafetyNotes);

                        Section(col, "QUALITY", "#1e3a5f",
                        [
                            ("First-off records complete?", a.FirstOffRecordsComplete),
                            ("Non-conformance procedure followed?", a.NcProcedureFollowed ?? a.NcCaptureTrended),
                            ("Quality gates maintained?", a.QualityGatesMaintained),
                        ], a.QualityNotes);

                        var peopleNotes = string.IsNullOrWhiteSpace(a.LastTeamMeeting)
                            ? a.PeopleNotes
                            : $"Last team meeting: {a.LastTeamMeeting}"
                              + (string.IsNullOrWhiteSpace(a.PeopleNotes) ? "" : $"\n{a.PeopleNotes}");

                        Section(col, "PEOPLE & WELLBEING", "#4a1d1d",
                        [
                            ("Operator check — visibility of leaders?", a.LeaderVisibilityCheck ?? a.AbsenceManagedProactively),
                            ("Training matrix current?", a.TrainingMatrixCurrent),
                        ], peopleNotes);

                        Section(col, "STANDARDS & HOUSEKEEPING", "#1e1b4b",
                        [
                            ("6S standard maintained?", a.SixSStandardMaintained),
                            ("TPM schedule followed?", a.TpmScheduleFollowed),
                            ("Standard work maintained across the area?", a.StandardWorkMaintained ?? a.StandardWorkVisible),
                        ], a.StandardsNotes);

                        Section(col, "PERFORMANCE", "#1e1b4b",
                        [
                            ("Tracking against weekly plan?", a.TrackingAgainstWeeklyPlan),
                            ("Improvement actions progressing?", a.ImprovementActionsProgressing),
                        ], a.PerformanceNotes);

                        col.Item().Element(c => RenderFindingsSignOff(c, findings =>
                        {
                            findings.Note("Good practice observed", a.GoodPracticeObserved, GreenBg);
                            findings.Note("Areas for improvement", a.AreasForImprovement, AmberBg);
                            findings.Note("Actions raised", a.ActionsRaised);
                            findings.Signatures(("Auditor signature", a.AuditorSignature));
                        }));

                        col.Item().Element(c => SeniorScoreFooter(c, overall, a.OverallVerdict));
                    });
                    page.Footer().AlignCenter().Text($"Production Audit System — {DateTime.UtcNow:dd MMM yyyy HH:mm} UTC").FontSize(8).FontColor(MidGray);
                });
            }).GeneratePdf();
        }

        public byte[] GenerateTeamMeeting(
            TeamMeeting m, List<TeamMeetingProblem> problems,
            List<TeamMeetingAttendee> team, List<TeamMeetingAttendee> group, List<TeamMeetingGuest> guests)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            const string ink = "#1a1a1a";      // text + table borders (0.75pt)
            const string labelBg = "#f3f4f6";   // label cells
            const string meta = "#6b7280";      // secondary text
            const string bannerGray = "#c3c3c3";
            const string bannerTop = "#6b6b8f";
            const string hair = "#e5e7eb";

            var docNo = DocNo(DocumentFormTypes.TeamMeeting);
            string V(string? s) => string.IsNullOrWhiteSpace(s) ? "" : s.Trim();

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(26);
                    page.DefaultTextStyle(x => x.FontSize(8.2f).FontFamily("Arial").FontColor(ink));
                    page.Footer().Element(c => DocFooter(c, docNo));

                    page.Content().Column(col =>
                    {
                        // ---- Header banner ----
                        col.Item().BorderTop(2).BorderColor(bannerTop).Background(bannerGray).Row(row =>
                        {
                            row.AutoItem().BorderRight(0.5f).BorderColor("#adadad").PaddingVertical(5).PaddingHorizontal(10)
                                .AlignMiddle().Column(c =>
                            {
                                c.Item().Text("RITTAL-CSM Ltd.").FontSize(8.5f).Bold().FontColor("#ffffff");
                                c.Item().Text("Plymouth").FontSize(8.5f).FontColor("#ffffff");
                            });
                            row.RelativeItem().PaddingHorizontal(12).AlignMiddle().Column(c =>
                            {
                                c.Item().Text("Team information").FontSize(12.5f).Bold();
                                c.Item().Text("Invitation for Team Meeting").FontSize(9f);
                            });
                            row.AutoItem().PaddingHorizontal(12).PaddingVertical(4).AlignMiddle().Row(lr =>
                            {
                                lr.Spacing(8);
                                lr.ConstantItem(34).Height(34).Background(bannerGray).Border(2).BorderColor("#8a3f8f")
                                    .AlignMiddle().AlignCenter().Text("RPS").FontSize(6.5f).Bold().FontColor("#3a3a3a");
                                lr.ConstantItem(40).AlignMiddle().Column(rc =>
                                {
                                    rc.Item().AlignRight().Column(bars =>
                                    {
                                        bars.Item().Width(30).Height(2.4f).Background("#c23a8a");
                                        bars.Item().PaddingTop(1).Width(22).Height(2.4f).Background("#7a3f9e");
                                        bars.Item().PaddingTop(1).Width(15).Height(2.4f).Background("#3a7fc1");
                                        bars.Item().PaddingTop(1).Width(8).Height(2.4f).Background("#3aa64f");
                                    });
                                    rc.Item().PaddingTop(2).Text("RITTAL").FontSize(8.5f).Bold().Italic();
                                });
                            });
                        });
                        if (!string.IsNullOrWhiteSpace(docNo))
                            col.Item().PaddingTop(3).PaddingBottom(8).AlignRight()
                                .Text("Doc no. " + docNo).FontSize(7f).FontColor(meta);
                        else
                            col.Item().PaddingBottom(4);

                        // ---- Meeting details ----
                        col.Item().Table(t =>
                        {
                            t.ColumnsDefinition(cd => { cd.ConstantColumn(84); cd.RelativeColumn(); cd.ConstantColumn(84); cd.RelativeColumn(); });
                            void Lab(string s, uint span = 1) => t.Cell().ColumnSpan(span).Border(0.75f).BorderColor(ink)
                                .Background(labelBg).Padding(3).Text(s).Bold();
                            void Val(string? s, uint span = 1) => t.Cell().ColumnSpan(span).Border(0.75f).BorderColor(ink)
                                .Padding(3).MinHeight(13).Text(V(s));

                            Lab("Supervisor"); Val(m.Supervisor, 3);
                            Lab("Cost Centre"); Val(m.CostCentre); Lab("Team"); Val(m.Team);
                            Lab("Topics"); Val("1–6, see legend below"); Lab("Location"); Val(m.Location);
                            Lab("Date / Time"); Val(m.MeetingDateTime); Lab("Compiled on"); Val(m.CompiledOn?.ToString("dd/MM/yyyy"));
                            Lab("Meeting Results"); Val(m.MeetingResults); Lab("Minutes Keeper"); Val(m.MinutesKeeper);
                        });

                        // ---- Topics legend (filled with this meeting's content) ----
                        col.Item().PaddingTop(8).Table(t =>
                        {
                            t.ColumnsDefinition(cd => { cd.RelativeColumn(); cd.RelativeColumn(); });
                            void Topic(string head, string? body) => t.Cell().Border(0.75f).BorderColor(ink).Padding(4).MinHeight(22)
                                .Text(txt => { txt.Span(head).Bold(); if (!string.IsNullOrWhiteSpace(body)) txt.Span(" — " + body.Trim()); });

                            Topic("1 = ACTIONS", m.Actions);
                            Topic("2 = H&S ISSUES", m.HealthSafety);
                            Topic("3 = CUSTOMER SATISFACTION", m.CustomerSatisfaction);
                            Topic("4 = KPIs", m.Kpis);
                            Topic("5 = EMPLOYEE SATISFACTION", m.EmployeeSatisfaction);
                            Topic("6 = A.O.B.", m.Aob);
                        });

                        // ---- Top 3 problems ----
                        col.Item().PaddingTop(8).Border(0.75f).BorderColor(ink).Column(box =>
                        {
                            box.Item().Background(ink).Padding(4)
                                .Text("TOP 3 PROBLEMS — please fill in problem, action, responsible and finish date")
                                .FontSize(8f).Bold().FontColor("#ffffff");
                            box.Item().Table(t =>
                            {
                                t.ColumnsDefinition(cd => { cd.ConstantColumn(18); cd.RelativeColumn(3); cd.RelativeColumn(3); cd.RelativeColumn(2); cd.ConstantColumn(56); cd.ConstantColumn(24); });
                                void Head(string s) => t.Cell().Border(0.75f).BorderColor(ink).Background(labelBg).Padding(3).Text(s).Bold();
                                void Cell(string s) => t.Cell().Border(0.75f).BorderColor(ink).Padding(3).MinHeight(15).Text(s);
                                Head("#"); Head("Problem"); Head("Action"); Head("Responsible"); Head("Finish Date"); Head("St.");
                                for (var i = 0; i < 3; i++)
                                {
                                    var p = i < problems.Count ? problems[i] : null;
                                    Cell((i + 1).ToString());
                                    Cell(V(p?.Problem)); Cell(V(p?.Action)); Cell(V(p?.Responsible));
                                    Cell(p?.FinishDate?.ToString("dd/MM/yy") ?? "");
                                    Cell(p != null ? TeamMeetingStatus.Short(p.Status) : "");
                                }
                            });
                            box.Item().Padding(4).Text(
                                "Status: 0 — problem identified   1 — owner named   2 — solution in process   3 — solution implemented   4 — standardised and sustainability assured")
                                .FontSize(6.6f).FontColor(meta);
                        });

                        // ---- Team / Group side by side ----
                        col.Item().PaddingTop(8).Row(r =>
                        {
                            r.Spacing(8);
                            r.RelativeItem().Element(c => AttendeeBox(c, "TEAM", team, 5, ink, labelBg));
                            r.RelativeItem().Element(c => AttendeeBox(c, "GROUP", group, 5, ink, labelBg));
                        });

                        // ---- Guests ----
                        col.Item().PaddingTop(8).Border(0.75f).BorderColor(ink).Column(box =>
                        {
                            box.Item().Background(ink).Padding(4).Text("GUEST").FontSize(8f).Bold().FontColor("#ffffff");
                            box.Item().Table(t =>
                            {
                                t.ColumnsDefinition(cd => { cd.RelativeColumn(2); cd.RelativeColumn(2); cd.ConstantColumn(90); });
                                void Head(string s) => t.Cell().Border(0.75f).BorderColor(ink).Background(labelBg).Padding(3).Text(s).Bold();
                                void Cell(string s) => t.Cell().Border(0.75f).BorderColor(ink).Padding(3).MinHeight(15).Text(s);
                                Head("Name"); Head("Department"); Head("Telephone");
                                var rows = Math.Max(1, guests.Count);
                                for (var i = 0; i < rows; i++)
                                {
                                    var g = i < guests.Count ? guests[i] : null;
                                    Cell(V(g?.Name)); Cell(V(g?.Department)); Cell(V(g?.Telephone));
                                }
                            });
                        });

                        // ---- Footer note ----
                        col.Item().PaddingTop(8).BorderTop(0.5f).BorderColor(hair).PaddingTop(6).Text(
                            "Absent team members are to be informed at the next attendance and are to sign and place the date in the “present” column.")
                            .FontSize(7f).Italic().FontColor(meta);
                    });
                });
            }).GeneratePdf();
        }

        static void AttendeeBox(IContainer container, string title, List<TeamMeetingAttendee> people, int minRows, string ink, string labelBg)
        {
            container.Border(0.75f).BorderColor(ink).Column(box =>
            {
                box.Item().Background(ink).Padding(4).Text(title).FontSize(8f).Bold().FontColor("#ffffff");
                box.Item().Table(t =>
                {
                    t.ColumnsDefinition(cd => { cd.ConstantColumn(50); cd.RelativeColumn(); });
                    t.Cell().Border(0.75f).BorderColor(ink).Background(labelBg).Padding(3).Text("Present").Bold();
                    t.Cell().Border(0.75f).BorderColor(ink).Background(labelBg).Padding(3).Text("Name").Bold();
                    var rows = Math.Max(minRows, people.Count);
                    for (var i = 0; i < rows; i++)
                    {
                        var p = i < people.Count ? people[i] : null;
                        t.Cell().Border(0.75f).BorderColor(ink).Padding(3).MinHeight(14).AlignCenter()
                            .Text(p == null ? "" : (p.Present ? "✓" : "")).Bold();
                        t.Cell().Border(0.75f).BorderColor(ink).Padding(3).MinHeight(14)
                            .Text(p?.Name ?? "");
                    }
                });
            });
        }

        static void SeniorScoreFooter(IContainer container, int overall, string? verdict)
        {
            var color = SeniorAuditScoring.GaugeColor(overall);
            container.Border(1.5f).BorderColor(DarkGray).Background(LightGray).Padding(24).AlignCenter().Column(col =>
            {
                col.Item().Text("OVERALL SCORE").FontSize(11).Bold().FontColor(MidGray);
                col.Item().Height(10);
                col.Item().Text($"{overall}%").FontSize(48).Bold().FontColor(color);
                if (!string.IsNullOrWhiteSpace(verdict))
                {
                    col.Item().Height(8);
                    col.Item().Text(verdict.ToUpperInvariant()).FontSize(22).Bold().FontColor(DarkGray);
                }
            });
        }

        static void ScoreRow(TableDescriptor t, byte? score)
        {
            t.Cell().Element(c =>
            {
                var bg = score == 2 ? GreenBg : score == 1 ? AmberBg : score == 0 ? RedBg : LightGray;
                var fg = score == 2 ? GreenText : score == 1 ? AmberText : score == 0 ? RedText : MidGray;
                c.Background(bg).Padding(2).AlignCenter()
                    .Text(SeniorAuditScoring.ScoreLabel(score)).FontColor(fg).Bold().FontSize(8);
            });
        }

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
                    col.Item().Text("Production Audit System").FontSize(11).FontColor("#6b7280");
                });
                row.ConstantItem(6).Background(Red);
            });
        }

        static void ComposeContent(IContainer c, ShiftSubmission s)
        {
            c.Column(col =>
            {
                col.Spacing(10);

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
                        t.Cell().Element(ValueCell).Text(string.IsNullOrWhiteSpace(s.CoveringFor)
                            ? s.TeamLeaderDisplay
                            : $"{s.TeamLeaderDisplay} (covering for {s.CoveringFor})");
                        t.Cell().Element(LabelCell).Text("Area");
                        t.Cell().Element(ValueCell).Text(s.Area ?? "—");
                        t.Cell().Element(LabelCell).Text("Hours completed");
                        t.Cell().Element(ValueCell).Text(s.HoursCompleted.ToString());
                        t.Cell().Element(LabelCell).Text("");
                        t.Cell().Element(ValueCell).Text("");
                    });
                });

                if (s.Hours?.Any() == true)
                {
                    var isSheet = AreaList.IsSheetmetal(s.Area);
                    col.Item().Border(0.5f).BorderColor("#e5e7eb").Column(inner =>
                    {
                        inner.Item().Background("#1c2b1e").Padding(6)
                            .Text(isSheet ? "2-HOURLY CHECKS" : "HOURLY CHECKS").FontSize(8).Bold().FontColor("#fff");
                        foreach (var h in s.Hours.OrderBy(x => x.HourNumber))
                        {
                            inner.Item().BorderBottom(0.5f).BorderColor("#e5e7eb").Padding(6).Column(hcol =>
                            {
                                var label = isSheet
                                    ? $"Check {h.HourNumber} · Hrs {h.HourNumber * 2 - 1}–{h.HourNumber * 2}"
                                    : $"Hour {h.HourNumber}";
                                hcol.Item().Text(label).FontSize(8).Bold().FontColor("#1a1a1a");
                                hcol.Item().Table(t =>
                                {
                                    t.ColumnsDefinition(cd => { cd.RelativeColumn(3); cd.RelativeColumn(); });
                                    BoolRow(t, "Hazards observed", h.HazardsObserved);
                                    BoolRow(t, "Unsafe behaviours", h.UnsafeBehaviours);
                                    BoolRow(t, "Positive behaviours", h.PositiveBehaviours);
                                    BoolRow(t, "Quality checks completed", h.QualityChecksCompleted);
                                    BoolRow(t, "Deviations escalated", h.DeviationsEscalated);
                                    BoolRow(t, "Non-compliance addressed", h.NonComplianceAddressed);
                                    BoolRow(t, "Tools match inspection plan", h.ToolsMatchInspectionPlan);
                                    if (h.PreKitBreakInFollowed.HasValue)
                                        BoolRow(t, "Pre-kitting & break-in processes followed", h.PreKitBreakInFollowed);
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

                col.Item().Border(0.5f).BorderColor("#e5e7eb").Column(inner =>
                {
                    inner.Item().Background("#f3f4f6").Padding(6)
                        .Text("SIGNATURE").FontSize(8).Bold().FontColor("#1a1a1a");
                    inner.Item().Padding(8).Row(r =>
                    {
                        r.RelativeItem().Text("Outgoing TL").FontSize(8).FontColor("#6b7280");
                        r.RelativeItem().Text(s.OutgoingTLSignature ?? "—").FontSize(9).Bold();
                    });
                });
            });
        }

        static void ComposeFooter(IContainer c)
        {
            c.Row(row =>
            {
                row.RelativeItem().Text(t =>
                    t.Span($"Production Audit System — {DateTime.UtcNow:dd MMM yyyy HH:mm} UTC").FontColor("#6b7280").FontSize(8));
            });
        }

        // Shared footer for documents that render their own header (e.g. the
        // Team Meeting form): controlled-doc number left, page numbers right.
        static void DocFooter(IContainer c, string? docNo)
        {
            c.PaddingTop(6).BorderTop(0.5f).BorderColor("#e5e7eb").PaddingTop(6).Row(row =>
            {
                row.RelativeItem().Text(t =>
                {
                    if (!string.IsNullOrWhiteSpace(docNo))
                        t.Span("Doc no. " + docNo).FontColor("#6b7280").FontSize(8);
                });
                row.AutoItem().AlignRight().Text(t =>
                {
                    t.Span("Page ").FontColor("#6b7280").FontSize(8);
                    t.CurrentPageNumber().FontColor("#6b7280").FontSize(8);
                    t.Span(" of ").FontColor("#6b7280").FontSize(8);
                    t.TotalPages().FontColor("#6b7280").FontSize(8);
                });
            });
        }

        static IContainer LabelCell(IContainer c) => c.PaddingVertical(3).PaddingRight(8);
        static IContainer ValueCell(IContainer c) => c.PaddingVertical(3);

        sealed class FindingsSignOffBuilder(ColumnDescriptor col)
        {
            public void Note(string label, string? text, string? background = null)
            {
                if (string.IsNullOrWhiteSpace(text)) return;
                col.Item().PaddingTop(6).Text(label).FontSize(10).Bold().FontColor(DarkGray);

                var lines = text.Replace("\r\n", "\n").Split('\n')
                    .Select(l => l.Trim())
                    .Where(l => l.Length > 0)
                    .ToList();
                var isList = lines.Count > 1;

                col.Item().PaddingTop(6)
                    .Background(background ?? LightGray)
                    .Border(0.5f).BorderColor(BorderGray)
                    .Padding(14)
                    .Column(lc =>
                    {
                        lc.Spacing(6);
                        foreach (var line in lines)
                        {
                            if (line.StartsWith("---") && line.EndsWith("---"))
                            {
                                lc.Item().PaddingTop(4).Text(line.Trim('-', ' ')).FontSize(8).Italic().FontColor(MidGray);
                            }
                            else if (isList)
                            {
                                lc.Item().Text(t =>
                                {
                                    t.Span("•  ").FontSize(10).FontColor(MidGray);
                                    t.Span(line).FontSize(10).LineHeight(1.35f).FontColor(DarkGray);
                                });
                            }
                            else
                            {
                                lc.Item().Text(line).FontSize(10).LineHeight(1.45f).FontColor(DarkGray);
                            }
                        }
                    });
                col.Item().Height(14);
            }

            public void Signatures(params (string Label, string? Value)[] signatures)
            {
                if (signatures.Length == 0) return;
                col.Item().PaddingTop(10).Text("Sign-off").FontSize(10).Bold().FontColor(DarkGray);
                col.Item().PaddingTop(8).Row(row =>
                {
                    foreach (var (label, value) in signatures)
                    {
                        row.RelativeItem().PaddingHorizontal(4).Border(1).BorderColor(BorderGray)
                            .Background("#fff").MinHeight(72).Padding(14).Column(sig =>
                            {
                                sig.Item().Text(label).FontSize(9).Bold().FontColor(MidGray);
                                sig.Item().Height(10);
                                sig.Item().Text(string.IsNullOrWhiteSpace(value) ? "—" : value)
                                    .FontSize(15).Bold().FontColor(DarkGray);
                            });
                    }
                });
            }
        }

        static void RenderFindingsSignOff(IContainer container, Action<FindingsSignOffBuilder> build)
        {
            container.Border(1).BorderColor(BorderGray).Column(inner =>
            {
                inner.Item().Background("#78350f").Padding(12)
                    .Text("FINDINGS & SIGN-OFF").FontSize(11).Bold().FontColor("#fff");
                inner.Item().Padding(18).Column(body => build(new FindingsSignOffBuilder(body)));
            });
        }

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
