using QuestPDF.Drawing;
using QuestPDF.Infrastructure;

namespace QuizSystem.Infrastructure.Services.Reports;

public static class QuestPdfFontRegistrar
{
    private static bool _isRegistered;

    public static void RegisterFonts()
    {
        if (_isRegistered)
            return;

        var baseDir = AppContext.BaseDirectory;
        var fontsDir = Path.Combine(baseDir, "Fonts");

        RegisterIfExists(Path.Combine(fontsDir, "Cairo-Regular.ttf"));
        RegisterIfExists(Path.Combine(fontsDir, "Cairo-Bold.ttf"));

        RegisterIfExists(Path.Combine(fontsDir, "Tajawal-Regular.ttf"));
        RegisterIfExists(Path.Combine(fontsDir, "Tajawal-Bold.ttf"));

        RegisterIfExists(Path.Combine(fontsDir, "NotoNaskhArabic-Regular.ttf"));
        RegisterIfExists(Path.Combine(fontsDir, "NotoNaskhArabic-Bold.ttf"));

        _isRegistered = true;
    }

    private static void RegisterIfExists(string path)
    {
        if (File.Exists(path))
            FontManager.RegisterFont(File.OpenRead(path));
    }
}