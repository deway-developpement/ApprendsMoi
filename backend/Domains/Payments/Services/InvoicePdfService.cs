using backend.Database.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace backend.Domains.Payments.Services;

public interface IInvoicePdfService {
    Task<byte[]> GenerateInvoicePdfAsync(Invoice invoice);
}

public class InvoicePdfService : IInvoicePdfService {
    // Informations légales de l'entreprise
    private const string CompanyName = "ApprendsMoi";
    private const string CompanyTagline = "Plateforme de cours particuliers";
    private const string CompanyAddress1 = "30-32 Avenue de la République";
    private const string CompanyAddress2 = "94800 Villejuif, France";
    private const string CompanyEmail = "dev.apprendsmoi@gmail.com";
    private const string CompanyPhone = "+33 1 23 45 67 89";
    private const string CompanyWebsite = "www.apprendsmoi.fr";
    
    private const string CompanySiret = "123 456 789 00012";
    private const string CompanyRcs = "RCS Créteil 123 456 789";
    private const string CompanyTvaNumber = "FR12 345 678 901";
    
    // Couleurs du thème
    private const string ColorPrimary = "#1a365d";
    private const string ColorSecondary = "#f97316";
    private const string ColorSuccess = "#4EE381";
    private const string ColorDanger = "#ef4444";
    private const string ColorGray = "#6b7280";
    private const string ColorLightGray = "#e5e7eb";
    private const string ColorLightBlue = "#f0f9ff";
    
    // Conditions de paiement
    private const string PaymentTerms = "Paiement à réception";
    private const decimal LatePenaltyRate = 9.0m;
    private const decimal RecoveryFee = 40.0m;
    
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
                column.Item().Text(CompanyName)
                    .FontSize(24)
                    .FontColor(ColorPrimary)
                    .Bold();
                
                column.Item().Text(CompanyTagline)
                    .FontSize(10)
                    .FontColor(ColorGray);
                
                column.Item().PaddingTop(10).Text(CompanyAddress1)
                    .FontSize(9);
                column.Item().Text(CompanyAddress2)
                    .FontSize(9);
                column.Item().Text(CompanyEmail)
                    .FontSize(9)
                    .FontColor(ColorSecondary);
                
                column.Item().PaddingTop(8).Text(text => {
                    text.Span("SIRET : ").FontSize(8);
                    text.Span(CompanySiret).FontSize(8).Bold();
                });
                column.Item().Text(text => {
                    text.Span("TVA : ").FontSize(8);
                    text.Span(CompanyTvaNumber).FontSize(8).Bold();
                });
            });

            // Right: Invoice info
            row.RelativeItem().AlignRight().Column(column => {
                column.Item().Border(1).BorderColor(ColorPrimary)
                    .Background(ColorLightBlue)
                    .Padding(10)
                    .Column(innerColumn => {
                        innerColumn.Item().Text("FACTURE")
                            .FontSize(18)
                            .Bold()
                            .FontColor(ColorPrimary);
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
                        InvoiceStatus.PAID => ColorSuccess,
                        InvoiceStatus.PENDING => ColorSecondary,
                        InvoiceStatus.CANCELLED => ColorDanger,
                        _ => ColorGray
                    };

                    col.Item().Background(statusColor)
                        .Padding(8)
                        .Text(invoice.Status.ToString())
                        .FontSize(11)
                        .Bold()
                        .FontColor(Colors.White);
                });
            });

            column.Item().PaddingVertical(20).LineHorizontal(1).LineColor(ColorLightGray);

            // Client information
            column.Item().PaddingBottom(20).Column(col => {
                col.Item().Text("Facturé à :")
                    .FontSize(11)
                    .Bold()
                    .FontColor(ColorPrimary);
                col.Item().PaddingTop(5).Text(invoice.Parent?.User?.GetFullName())
                    .FontSize(10);
                col.Item().Text(invoice.Parent?.User?.Email ?? "")
                    .FontSize(10)
                    .FontColor(ColorGray);
            });

            // Course details
            column.Item().PaddingBottom(15).Text("Détails du cours")
                .FontSize(12)
                .Bold()
                .FontColor(ColorPrimary);

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
                    header.Cell().Background(ColorPrimary).Padding(8).Text("Description")
                        .FontColor(Colors.White).Bold();
                    header.Cell().Background(ColorPrimary).Padding(8).Text("Qté")
                        .FontColor(Colors.White).Bold();
                    header.Cell().Background(ColorPrimary).Padding(8).Text("Prix unitaire")
                        .FontColor(Colors.White).Bold();
                    header.Cell().Background(ColorPrimary).Padding(8).Text("Total")
                        .FontColor(Colors.White).Bold();
                });

                // Content
                table.Cell().Border(1).BorderColor(ColorLightGray).Padding(8)
                    .Column(col => {
                        col.Item().Text(invoice.Course?.Subject?.Name ?? "Cours particulier")
                            .Bold();
                        col.Item().Text($"Enseignant : {invoice.Course?.Teacher?.User?.GetFullName()}")
                            .FontSize(9)
                            .FontColor(ColorGray);
                    });
                
                table.Cell().Border(1).BorderColor(ColorLightGray).Padding(8)
                    .AlignCenter()
                    .AlignMiddle()
                    .Text("1");
                
                table.Cell().Border(1).BorderColor(ColorLightGray).Padding(8)
                    .AlignRight()
                    .AlignMiddle()
                    .Text($"{invoice.Amount:F2} €");
                
                table.Cell().Border(1).BorderColor(ColorLightGray).Padding(8)
                    .AlignRight()
                    .AlignMiddle()
                    .Text($"{invoice.Amount:F2} €")
                    .Bold();
            });

            column.Item().PaddingTop(20).AlignRight().Column(col => {
                col.Item().Row(row => {
                    row.AutoItem().Width(150).Text("Sous-total HT :")
                        .FontSize(10);
                    row.AutoItem().Width(100).AlignRight().Text($"{invoice.Amount:F2} €")
                        .FontSize(10);
                });

                col.Item().PaddingTop(5).Row(row => {
                    row.AutoItem().Width(150).Text("TVA non applicable :")
                        .FontSize(9)
                        .FontColor(ColorGray);
                    row.AutoItem().Width(100).AlignRight().Text("Art. 293 B du CGI")
                        .FontSize(8)
                        .FontColor(ColorGray);
                });

                col.Item().PaddingTop(10).Border(1).BorderColor(ColorPrimary)
                    .Background(ColorLightBlue)
                    .Padding(10)
                    .Row(row => {
                        row.AutoItem().Width(150).Text("TOTAL TTC :")
                            .FontSize(12)
                            .Bold()
                            .FontColor(ColorPrimary);
                        row.AutoItem().Width(100).AlignRight().Text($"{invoice.Amount:F2} €")
                            .FontSize(14)
                            .Bold()
                            .FontColor(ColorPrimary);
                    });
            });

            // Payment info
            if (invoice.Status == InvoiceStatus.PAID && invoice.PaymentIntentId != null) {
                column.Item().PaddingTop(20).Column(col => {
                    col.Item().Text("Informations de paiement")
                        .FontSize(11)
                        .Bold()
                        .FontColor(ColorSuccess);
                    col.Item().PaddingTop(5).Text($"ID de transaction : {invoice.PaymentIntentId}")
                        .FontSize(9)
                        .FontColor(ColorGray);
                });
            }

            // Payment terms and legal notices
            column.Item().PaddingTop(25).Column(col => {
                col.Item().Text("Conditions de paiement")
                    .FontSize(10)
                    .Bold()
                    .FontColor(ColorPrimary);
                
                col.Item().PaddingTop(8).Text(PaymentTerms)
                    .FontSize(9);
                
                col.Item().PaddingTop(5).Text(text => {
                    text.Span("En cas de retard de paiement, seront exigibles une indemnité calculée sur la base de ")
                        .FontSize(8)
                        .FontColor(ColorGray);
                    text.Span($"{LatePenaltyRate:F1}% ")
                        .FontSize(8)
                        .FontColor(ColorGray)
                        .Bold();
                    text.Span("(trois fois le taux d'intérêt légal) ainsi qu'une indemnité forfaitaire pour frais de recouvrement de ")
                        .FontSize(8)
                        .FontColor(ColorGray);
                    text.Span($"{RecoveryFee:F0}€")
                        .FontSize(8)
                        .FontColor(ColorGray)
                        .Bold();
                    text.Span(", conformément aux articles L. 441-10 et D. 441-5 du Code de commerce.")
                        .FontSize(8)
                        .FontColor(ColorGray);
                });
            });

            // Footer notes
            column.Item().PaddingTop(15).Column(col => {
                col.Item().Text("Cette facture est générée électroniquement et ne nécessite pas de signature.")
                    .FontSize(8)
                    .FontColor(ColorGray)
                    .Italic();
            });
        });
    }

    private void ComposeFooter(IContainer container) {
        container.AlignCenter().Column(column => {
            column.Item().LineHorizontal(1).LineColor(ColorLightGray);
            column.Item().PaddingTop(10).Text(text => {
                text.Span($"{CompanyName} - SIRET : {CompanySiret} - {CompanyRcs}")
                    .FontSize(8)
                    .FontColor(ColorGray);
            });
            column.Item().Text($"N° TVA Intracommunautaire : {CompanyTvaNumber} - TVA non applicable, art. 293 B du CGI")
                .FontSize(7)
                .FontColor(ColorGray);
            column.Item().PaddingTop(3).Text($"{CompanyWebsite} • {CompanyEmail} • {CompanyPhone}")
                .FontSize(8)
                .FontColor(ColorGray);
        });
    }
}
