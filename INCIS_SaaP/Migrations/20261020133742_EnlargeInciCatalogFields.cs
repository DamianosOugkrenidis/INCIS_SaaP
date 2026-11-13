using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INCIS_SaaP.Migrations
{
    /// <inheritdoc />
    public partial class EnlargeInciCatalogFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "INCIs",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "ECNumber",
                table: "INCIs",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CASNumber",
                table: "INCIs",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // ============================================================
            // ΑΣΦΑΛΗΣ DOWN (μη-καταστροφικός)
            // ------------------------------------------------------------
            // Η αρχική έκδοση συρρίκνωνε Name 500->200, EC 200->100, CAS 200->100.
            // Με τα δεδομένα του ALL_INCIS_NEW_FILE (Name έως 1903 χαρ.) αυτό προκαλεί
            // SQL error 2628 (String or binary data would be truncated) και αποτυγχάνει.
            //
            // Επιλογή: ΔΕΝ συρρικνώνουμε τις στήλες. Οι ευρύτερες στήλες είναι
            // μη-καταστροφικές/συμβατές και διατηρούν τα δεδομένα ακέραια.
            // Η επαναφορά των πρόσθετων πεδίων (FullName/Origin/...) γίνεται από τα
            // αντίστοιχα migrations. Για πλήρη επαναφορά με διατήρηση δεδομένων,
            // προτιμάται restore από επαληθευμένο backup (βλ. DBA runbook).
            // ============================================================
        }
    }
}
