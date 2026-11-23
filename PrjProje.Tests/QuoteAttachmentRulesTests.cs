using System.Text;
using PrjProje.Application.Common;

namespace PrjProje.Tests;

public class QuoteAttachmentRulesTests
{
    private static readonly byte[] Pdf = Encoding.ASCII.GetBytes("%PDF-1.7\n");
    private static readonly byte[] Exe = [0x4D, 0x5A, 0x90, 0x00, 0x03];
    private static readonly byte[] Zip = [0x50, 0x4B, 0x03, 0x04, 0x14];
    private static readonly byte[] Step = Encoding.ASCII.GetBytes("ISO-10303-21;\nHEADER;");
    private const long OneMb = 1024 * 1024;

    [Theory]
    [InlineData("teknik-resim.pdf")]
    [InlineData("TEKNIK-RESIM.PDF")]
    public void Real_pdf_is_accepted(string name) => Assert.Null(QuoteAttachmentRules.Validate(name, OneMb, Pdf));

    [Fact]
    public void Executable_renamed_to_pdf_is_rejected() =>
        Assert.Equal("Validation.FileContent", QuoteAttachmentRules.Validate("teklif.pdf", OneMb, Exe));

    [Theory]
    [InlineData("kurulum.exe")]
    [InlineData("script.js")]
    [InlineData("parca.igs")]
    [InlineData("noextension")]
    public void Types_outside_the_list_are_rejected(string name) =>
        Assert.Equal("Validation.FileType", QuoteAttachmentRules.Validate(name, OneMb, Pdf));

    [Fact]
    public void Twenty_five_megabytes_is_too_large() =>
        Assert.Equal("Validation.FileSize", QuoteAttachmentRules.Validate("buyuk.pdf", 25 * OneMb, Pdf));

    [Fact]
    public void Exactly_ten_megabytes_is_allowed() => Assert.Null(QuoteAttachmentRules.Validate("sinir.pdf", 10 * OneMb, Pdf));

    [Fact]
    public void Empty_file_is_rejected() => Assert.Equal("Validation.FileType", QuoteAttachmentRules.Validate("bos.pdf", 0, []));

    [Theory]
    [InlineData("model.step")]
    [InlineData("model.stp")]
    [InlineData("plan.dxf")]
    public void Plain_text_cad_files_are_accepted(string name) => Assert.Null(QuoteAttachmentRules.Validate(name, OneMb, Step));

    [Fact]
    public void Zip_is_accepted() => Assert.Null(QuoteAttachmentRules.Validate("cizimler.zip", OneMb, Zip));

    [Fact]
    public void Allowed_list_matches_the_spec() =>
        Assert.Equal([".dwg", ".dxf", ".jpeg", ".jpg", ".pdf", ".png", ".step", ".stp", ".zip"], QuoteAttachmentRules.AllowedExtensions.Order());
}
