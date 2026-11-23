using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrjProje.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Capability became Service. Hand-written as renames so existing rows survive; the scaffold dropped and recreated the tables.
    /// </summary>
    public partial class RenameCapabilityToService : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(name: "FK_Capabilities_GalleryCategories_GalleryCategoryId", table: "Capabilities");
            migrationBuilder.DropForeignKey(name: "FK_CapabilityProductCategories_Capabilities_CapabilityId", table: "CapabilityProductCategories");
            migrationBuilder.DropForeignKey(name: "FK_CapabilityProductCategories_ProductCategories_ProductCategoriesId", table: "CapabilityProductCategories");
            migrationBuilder.DropForeignKey(name: "FK_CapabilityTranslations_Capabilities_CapabilityId", table: "CapabilityTranslations");
            migrationBuilder.DropPrimaryKey(name: "PK_CapabilityTranslations", table: "CapabilityTranslations");
            migrationBuilder.DropPrimaryKey(name: "PK_CapabilityProductCategories", table: "CapabilityProductCategories");
            migrationBuilder.DropPrimaryKey(name: "PK_Capabilities", table: "Capabilities");
            migrationBuilder.DropIndex(name: "IX_CapabilityProductCategories_ProductCategoriesId", table: "CapabilityProductCategories");

            migrationBuilder.RenameTable(name: "Capabilities", newName: "Services");
            migrationBuilder.RenameTable(name: "CapabilityTranslations", newName: "ServiceTranslations");
            migrationBuilder.RenameTable(name: "CapabilityProductCategories", newName: "ServiceProductCategories");
            migrationBuilder.RenameColumn(name: "CapabilityId", table: "ServiceTranslations", newName: "ServiceId");
            migrationBuilder.RenameColumn(name: "CapabilityId", table: "ServiceProductCategories", newName: "ServiceId");
            migrationBuilder.RenameIndex(name: "IX_Capabilities_GalleryCategoryId", table: "Services", newName: "IX_Services_GalleryCategoryId");
            migrationBuilder.RenameIndex(name: "IX_CapabilityTranslations_CapabilityId_LanguageCode", table: "ServiceTranslations", newName: "IX_ServiceTranslations_ServiceId_LanguageCode");
            migrationBuilder.RenameIndex(name: "IX_CapabilityTranslations_LanguageCode_Slug", table: "ServiceTranslations", newName: "IX_ServiceTranslations_LanguageCode_Slug");

            migrationBuilder.AddPrimaryKey(name: "PK_Services", table: "Services", column: "Id");
            migrationBuilder.AddPrimaryKey(name: "PK_ServiceTranslations", table: "ServiceTranslations", column: "Id");
            migrationBuilder.AddPrimaryKey(name: "PK_ServiceProductCategories", table: "ServiceProductCategories", columns: new[] { "ProductCategoriesId", "ServiceId" });
            migrationBuilder.CreateIndex(name: "IX_ServiceProductCategories_ServiceId", table: "ServiceProductCategories", column: "ServiceId");

            migrationBuilder.AddForeignKey(
                name: "FK_Services_GalleryCategories_GalleryCategoryId", table: "Services", column: "GalleryCategoryId",
                principalTable: "GalleryCategories", principalColumn: "Id", onDelete: ReferentialAction.SetNull);
            migrationBuilder.AddForeignKey(
                name: "FK_ServiceTranslations_Services_ServiceId", table: "ServiceTranslations", column: "ServiceId",
                principalTable: "Services", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
            migrationBuilder.AddForeignKey(
                name: "FK_ServiceProductCategories_Services_ServiceId", table: "ServiceProductCategories", column: "ServiceId",
                principalTable: "Services", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
            migrationBuilder.AddForeignKey(
                name: "FK_ServiceProductCategories_ProductCategories_ProductCategoriesId", table: "ServiceProductCategories", column: "ProductCategoriesId",
                principalTable: "ProductCategories", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(name: "FK_Services_GalleryCategories_GalleryCategoryId", table: "Services");
            migrationBuilder.DropForeignKey(name: "FK_ServiceTranslations_Services_ServiceId", table: "ServiceTranslations");
            migrationBuilder.DropForeignKey(name: "FK_ServiceProductCategories_Services_ServiceId", table: "ServiceProductCategories");
            migrationBuilder.DropForeignKey(name: "FK_ServiceProductCategories_ProductCategories_ProductCategoriesId", table: "ServiceProductCategories");
            migrationBuilder.DropPrimaryKey(name: "PK_ServiceProductCategories", table: "ServiceProductCategories");
            migrationBuilder.DropPrimaryKey(name: "PK_ServiceTranslations", table: "ServiceTranslations");
            migrationBuilder.DropPrimaryKey(name: "PK_Services", table: "Services");
            migrationBuilder.DropIndex(name: "IX_ServiceProductCategories_ServiceId", table: "ServiceProductCategories");

            migrationBuilder.RenameIndex(name: "IX_ServiceTranslations_LanguageCode_Slug", table: "ServiceTranslations", newName: "IX_CapabilityTranslations_LanguageCode_Slug");
            migrationBuilder.RenameIndex(name: "IX_ServiceTranslations_ServiceId_LanguageCode", table: "ServiceTranslations", newName: "IX_CapabilityTranslations_CapabilityId_LanguageCode");
            migrationBuilder.RenameIndex(name: "IX_Services_GalleryCategoryId", table: "Services", newName: "IX_Capabilities_GalleryCategoryId");
            migrationBuilder.RenameColumn(name: "ServiceId", table: "ServiceProductCategories", newName: "CapabilityId");
            migrationBuilder.RenameColumn(name: "ServiceId", table: "ServiceTranslations", newName: "CapabilityId");
            migrationBuilder.RenameTable(name: "ServiceProductCategories", newName: "CapabilityProductCategories");
            migrationBuilder.RenameTable(name: "ServiceTranslations", newName: "CapabilityTranslations");
            migrationBuilder.RenameTable(name: "Services", newName: "Capabilities");

            migrationBuilder.AddPrimaryKey(name: "PK_Capabilities", table: "Capabilities", column: "Id");
            migrationBuilder.AddPrimaryKey(name: "PK_CapabilityTranslations", table: "CapabilityTranslations", column: "Id");
            migrationBuilder.AddPrimaryKey(name: "PK_CapabilityProductCategories", table: "CapabilityProductCategories", columns: new[] { "CapabilityId", "ProductCategoriesId" });
            migrationBuilder.CreateIndex(name: "IX_CapabilityProductCategories_ProductCategoriesId", table: "CapabilityProductCategories", column: "ProductCategoriesId");

            migrationBuilder.AddForeignKey(
                name: "FK_Capabilities_GalleryCategories_GalleryCategoryId", table: "Capabilities", column: "GalleryCategoryId",
                principalTable: "GalleryCategories", principalColumn: "Id", onDelete: ReferentialAction.SetNull);
            migrationBuilder.AddForeignKey(
                name: "FK_CapabilityTranslations_Capabilities_CapabilityId", table: "CapabilityTranslations", column: "CapabilityId",
                principalTable: "Capabilities", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
            migrationBuilder.AddForeignKey(
                name: "FK_CapabilityProductCategories_Capabilities_CapabilityId", table: "CapabilityProductCategories", column: "CapabilityId",
                principalTable: "Capabilities", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
            migrationBuilder.AddForeignKey(
                name: "FK_CapabilityProductCategories_ProductCategories_ProductCategoriesId", table: "CapabilityProductCategories", column: "ProductCategoriesId",
                principalTable: "ProductCategories", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
        }
    }
}
