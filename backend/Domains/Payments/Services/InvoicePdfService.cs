using backend.Database.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace backend.Domains.Payments.Services;

public interface IInvoicePdfService {
    Task<byte[]> GenerateInvoicePdfAsync(Invoice invoice);
}

public class InvoicePdfService : IInvoicePdfService {
    public InvoicePdfService() {
        // QuestPDF Community License - for development/testing
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<byte[]> GenerateInvoicePdfAsync(Invoice invoice) {
        return await Task.Run(() => {
            var document = Document.Create(container => {
                container.Page(page => {
                    page.Size(PageSizes.A4);
                    page.Margin(50);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                    page.Header().Element(ComposeHeader);
                    page.Content().Element(container => ComposeContent(container, invoice));
                    page.Footer().Element(ComposeFooter);
                });
            });

            return document.GeneratePdf();
        });
    }

    private void ComposeHeader(IContainer container) {
        container.Row(row => {
            // Left: Company branding
            row.RelativeItem().Column(column => {
                column.Item().Text("ApprendsMoi")
                    .FontSize(24)
                    .FontColor("#1a365d")
                    .Bold();
                
                column.Item().Text("Plateforme de cours particuliers")
                    .FontSize(10)
                    .FontColor("#6b7280");
                
                column.Item().PaddingTop(10).Text("123 Rue de l'Éducation")
                    .FontSize(9);
                column.Item().Text("75001 Paris, France")
                    .FontSize(9);
                column.Item().Text("contact@apprendsmoi.fr")
                    .FontSize(9)
                    .FontColor("#f97316");
            });

            // Right: Invoice info
            row.RelativeItem().AlignRight().Column(column => {
                column.Item().Border(1).BorderColor("#1a365d")
                    .Background("#f0f9ff")
                    .Padding(10)
                    .Column(innerColumn => {
                        innerColumn.Item().Text("FACTURE")
                            .FontSize(18)
                            .Bold()
                            .FontColor("#1a365d");
                    });
            });
        });
    }

    private void ComposeContent(IContainer container, Invoice invoice) {
        container.PaddingVertical(20).Column(column => {
            // Invoice details section
            column.Item().Row(row => {
                row.RelativeItem().Column(col => {
                    col.Item().Text($"Facture N° {invoice.InvoiceNumber}")
                        .FontSize(12)
                        .Bold();
                    col.Item().PaddingTop(5).Text($"Date d'émission : {invoice.IssuedAt:dd/MM/yyyy}")
                        .FontSize(10);
                    if (invoice.PaidAt.HasValue) {
                        col.Item().Text($"Date de paiement : {invoice.PaidAt.Value:dd/MM/yyyy}")
                            .FontSize(10)
                            .FontColor("#4EE381");
                    }
                });

                row.RelativeItem().AlignRight().Column(col => {
                    var statusColor = invoice.Status switch {
                        InvoiceStatus.PAID => "#4EE381",
                        InvoiceStatus.PENDING => "#f97316",
                        InvoiceStatus.CANCELLED => "#ef4444",
                        _ => "#6b7280"
                    };

                    col.Item().Background(statusColor)
                        .Padding(8)
                        .Text(invoice.Status.ToString())
                        .FontSize(11)
                        .Bold()
                        .FontColor(Colors.White);
                });
            });

            column.Item().PaddingVertical(20).LineHorizontal(1).LineColor("#e5e7eb");

            // Client information
            column.Item().PaddingBottom(20).Column(col => {
                col.Item().Text("Facturé à :")
                    .FontSize(11)
                    .Bold()
                    .FontColor("#1a365d");
                col.Item().PaddingTop(5).Text(invoice.Parent?.User?.GetFullName() ?? "Client")
                    .FontSize(10);
                col.Item().Text(invoice.Parent?.User?.Email ?? "")
                    .FontSize(10)
                    .FontColor("#6b7280");
            });

            // Course details
            column.Item().PaddingBottom(15).Text("Détails du cours")
                .FontSize(12)
                .Bold()
                .FontColor("#1a365d");

            // Table
            column.Item().Table(table => {
                table.ColumnsDefinition(columns => {
                    columns.RelativeColumn(3); // Description
                    columns.RelativeColumn(1); // Quantité
                    columns.RelativeColumn(1); // Prix unitaire
                    columns.RelativeColumn(1); // Total
                });

                // Header
                table.Header(header => {
                    header.Cell().Background("#1a365d").Padding(8).Text("Description")
                        .FontColor(Colors.White).Bold();
                    header.Cell().Background("#1a365d").Padding(8).Text("Qté")
                        .FontColor(Colors.White).Bold();
                    header.Cell().Background("#1a365d").Padding(8).Text("Prix unitaire")
                        .FontColor(Colors.White).Bold();
                    header.Cell().Background("#1a365d").Padding(8).Text("Total")
                        .FontColor(Colors.White).Bold();
                });

                // Content
                table.Cell().Border(1).BorderColor("#e5e7eb").Padding(8)
                    .Column(col => {
                        col.Item().Text(invoice.Course?.Subject?.Name ?? "Cours particulier")
                            .Bold();
                        col.Item().Text($"Enseignant : {invoice.Course?.Teacher?.User?.GetFullName() ?? "N/A"}")
                            .FontSize(9)
                            .FontColor("#6b7280");
                    });
                
                table.Cell().Border(1).BorderColor("#e5e7eb").Padding(8)
                    .AlignCenter()
                    .AlignMiddle()
                    .Text("1");
                
                table.Cell().Border(1).BorderColor("#e5e7eb").Padding(8)
                    .AlignRight()
                    .AlignMiddle()
                    .Text($"{invoice.Amount:F2} €");
                
                table.Cell().Border(1).BorderColor("#e5e7eb").Padding(8)
                    .AlignRight()
                    .AlignMiddle()
                    .Text($"{invoice.Amount:F2} €")
                    .Bold();
            });

            column.Item().PaddingTop(20).AlignRight().Column(col => {
                col.Item().Row(row => {
                    row.AutoItem().Width(150).Text("Sous-total :")
                        .FontSize(10);
                    row.AutoItem().Width(100).AlignRight().Text($"{invoice.Amount:F2} €")
                        .FontSize(10);
                });

                col.Item().PaddingTop(5).Row(row => {
                    row.AutoItem().Width(150).Text("Commission plateforme :")
                        .FontSize(10)
                        .FontColor("#6b7280");
                    row.AutoItem().Width(100).AlignRight().Text($"-{invoice.Commission:F2} €")
                        .FontSize(10)
                        .FontColor("#6b7280");
                });

                col.Item().PaddingTop(10).Border(1).BorderColor("#1a365d")
                    .Background("#f0f9ff")
                    .Padding(10)
                    .Row(row => {
                        row.AutoItem().Width(150).Text("TOTAL TTC :")
                            .FontSize(12)
                            .Bold()
                            .FontColor("#1a365d");
                        row.AutoItem().Width(100).AlignRight().Text($"{invoice.Amount:F2} €")
                            .FontSize(14)
                            .Bold()
                            .FontColor("#1a365d");
                    });
            });

            // Payment info
            if (invoice.Status == InvoiceStatus.PAID && invoice.PaymentIntentId != null) {
                column.Item().PaddingTop(20).Column(col => {
                    col.Item().Text("Informations de paiement")
                        .FontSize(11)
                        .Bold()
                        .FontColor("#4EE381");
                    col.Item().PaddingTop(5).Text($"ID de transaction : {invoice.PaymentIntentId}")
                        .FontSize(9)
                        .FontColor("#6b7280");
                });
            }

            // Footer notes
            column.Item().PaddingTop(30).Column(col => {
                col.Item().Text("Notes et conditions")
                    .FontSize(10)
                    .Bold();
                col.Item().PaddingTop(5).Text("Merci d'avoir choisi ApprendsMoi pour vos cours particuliers.")
                    .FontSize(9);
                col.Item().Text("Cette facture est générée automatiquement et ne nécessite pas de signature.")
                    .FontSize(8)
                    .FontColor("#6b7280")
                    .Italic();
            });
        });
    }

    private void ComposeFooter(IContainer container) {
        container.AlignCenter().Column(column => {
            column.Item().LineHorizontal(1).LineColor("#e5e7eb");
            column.Item().PaddingTop(10).Text(text => {
                text.Span("ApprendsMoi SAS - SIRET : 123 456 789 00012 - TVA : FR12345678901")
                    .FontSize(8)
                    .FontColor("#6b7280");
            });
            column.Item().Text("www.apprendsmoi.fr • contact@apprendsmoi.fr • +33 1 23 45 67 89")
                .FontSize(8)
                .FontColor("#6b7280");
        });
    }
}
