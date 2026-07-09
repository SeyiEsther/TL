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

                        // Audit details
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

                        // H&S
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

                        // Quality
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

                        // Performance
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

                        // Morale
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

                        // Verdict
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

                        // Findings & Actions
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

                        // Sign-off
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
            var sections = answers.GroupBy(x => x.Section).ToList();

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
                                c.Item().Text($"HoD Audit — {HodAuditTypes.LabelFor(a.AuditType)}").FontSize(11).FontColor(MidGray);
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
                                t.Cell().Element(LabelCell).Text("Area to audit");
                                t.Cell().Element(ValueCell).Text(a.Department);
                                t.Cell().Element(LabelCell).Text("Auditor");
                                t.Cell().Element(ValueCell).Text(a.AuditorName);
                                t.Cell().Element(LabelCell).Text("Submitted");
                                t.Cell().Element(ValueCell).Text(a.SubmittedAt.ToLocalTime().ToString("dd MMM yyyy HH:mm"));
                            });
                        });

                        foreach (var sec in sections)
                        {
                            col.Item().Border(0.5f).BorderColor(BorderGray).Column(inner =>
                            {
                                inner.Item().Background(DarkGray).Padding(6).Text(sec.Key.ToUpperInvariant()).FontSize(8).Bold().FontColor("#fff");
                                inner.Item().Padding(8).Table(t =>
                                {
                                    t.ColumnsDefinition(cd => { cd.RelativeColumn(4); cd.RelativeColumn(); cd.RelativeColumn(2); });
                                    foreach (var q in sec)
                                    {
                                        t.Cell().PaddingVertical(2).Text(q.Label).FontColor(MidGray).FontSize(8);
                                        PassFailRow(t, q.Pass);
                                        t.Cell().PaddingVertical(2).Text(q.Evidence ?? "—").FontSize(8).FontColor(MidGray);
                                    }
                                });
                            });
                        }

                        col.Item().Border(0.5f).BorderColor(BorderGray).Column(inner =>
                        {
                            inner.Item().Background("#78350f").Padding(6).Text("FINDINGS & SIGN-OFF").FontSize(8).Bold().FontColor("#fff");
                            inner.Item().Padding(8).Column(sc =>
                            {
                                if (!string.IsNullOrWhiteSpace(a.ActionsRaised))
                                {
                                    sc.Item().Text("Actions raised").FontSize(8).Bold();
                                    sc.Item().Background(LightGray).Padding(4).Text(a.ActionsRaised).FontSize(8);
                                }
                                if (!string.IsNullOrWhiteSpace(a.GoodPractice))
                                {
                                    sc.Item().Text("Good practice").FontSize(8).Bold();
                                    sc.Item().Background(LightGray).Padding(4).Text(a.GoodPractice).FontSize(8);
                                }
                                sc.Item().Table(t =>
                                {
                                    t.ColumnsDefinition(cd => { cd.RelativeColumn(); cd.RelativeColumn(); });
                                    t.Cell().Element(LabelCell).Text("Auditor signature");
                                    t.Cell().Element(LabelCell).Text("TL signature");
                                    t.Cell().Element(ValueCell).Text(a.AuditorSignature ?? "—");
                                    t.Cell().Element(ValueCell).Text(a.TeamLeaderSignature ?? "—");
                                });
                            });
                        });

                        col.Item().Element(c => HodScoreFooter(c, a.TotalScore, a.MaxScore, band));
                    });
                    page.Footer().AlignCenter().Text($"Production Audit System — {DateTime.UtcNow:dd MMM yyyy HH:mm} UTC").FontSize(8).FontColor(MidGray);
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

        static void PassFailRow(TableDescriptor t, bool? pass)
        {
            t.Cell().Element(c =>
            {
                var bg = pass == true ? GreenBg : pass == false ? RedBg : LightGray;
                var fg = pass == true ? GreenText : pass == false ? RedText : MidGray;
                var lbl = pass == true ? "Pass (1)" : pass == false ? "Fail (0)" : "—";
                c.Background(bg).Padding(2).AlignCenter().Text(lbl).FontColor(fg).Bold().FontSize(8);
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

                        col.Item().Border(0.5f).BorderColor(BorderGray).Column(inner =>
                        {
                            inner.Item().Background("#78350f").Padding(6).Text("FINDINGS & SIGN-OFF").FontSize(8).Bold().FontColor("#fff");
                            inner.Item().Padding(8).Column(sc =>
                            {
                                void Note(string label, string? text)
                                {
                                    if (string.IsNullOrWhiteSpace(text)) return;
                                    sc.Item().Text(label).FontSize(8).Bold();
                                    sc.Item().Background(LightGray).Padding(4).Text(text).FontSize(8);
                                    sc.Item().Height(4);
                                }
                                Note("Good practice observed", a.GoodPracticeObserved);
                                Note("Areas for improvement", a.AreasForImprovement);
                                Note("Actions raised", a.ActionsRaised);
                                sc.Item().Table(t =>
                                {
                                    t.ColumnsDefinition(cd => { cd.RelativeColumn(); });
                                    t.Cell().Element(LabelCell).Text("Auditor signature");
                                    t.Cell().Element(ValueCell).Text(a.AuditorSignature ?? "—");
                                });
                            });
                        });

                        col.Item().Element(c => SeniorScoreFooter(c, overall, a.OverallVerdict));
                    });
                    page.Footer().AlignCenter().Text($"Production Audit System — {DateTime.UtcNow:dd MMM yyyy HH:mm} UTC").FontSize(8).FontColor(MidGray);
                });
            }).GeneratePdf();
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
                        t.Cell().Element(LabelCell).Text("Area");
                        t.Cell().Element(ValueCell).Text(s.Area ?? "—");
                        t.Cell().Element(LabelCell).Text("Hours completed");
                        t.Cell().Element(ValueCell).Text(s.HoursCompleted.ToString());
                        t.Cell().Element(LabelCell).Text("");
                        t.Cell().Element(ValueCell).Text("");
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
                    t.Span($"Production Audit System — {DateTime.UtcNow:dd MMM yyyy HH:mm} UTC").FontColor("#6b7280").FontSize(8));
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