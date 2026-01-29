using FluentMigrator;

namespace backend.Database.Migrations;

[Migration(20260129001)]
public class AddInvoiceVatFields : Migration {
    public override void Up() {
        Alter.Table("invoices")
            .AddColumn("amount_ht").AsDecimal(18, 2).NotNullable().WithDefaultValue(0)
            .AddColumn("vat_amount").AsDecimal(18, 2).NotNullable().WithDefaultValue(0);
        
        Execute.Sql(@"
            UPDATE invoices 
            SET amount_ht = amount / 1.20,
                vat_amount = amount - (amount / 1.20)
            WHERE amount_ht = 0 AND vat_amount = 0;
        ");
    }

    public override void Down() {
        Delete.Column("amount_ht").FromTable("invoices");
        Delete.Column("vat_amount").FromTable("invoices");
    }
}
