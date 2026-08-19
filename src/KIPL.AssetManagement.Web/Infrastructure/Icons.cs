using Microsoft.AspNetCore.Html;

namespace KIPL.AssetManagement.Web.Infrastructure;

/// <summary>The prototype's inline SVG icon set, kept as markup so the rail renders without JS.</summary>
public static class Icons
{
    private static readonly Dictionary<string, string> Paths = new()
    {
        ["grid"] = "<rect x='3' y='3' width='7' height='7' rx='1.5'/><rect x='14' y='3' width='7' height='7' rx='1.5'/><rect x='3' y='14' width='7' height='7' rx='1.5'/><rect x='14' y='14' width='7' height='7' rx='1.5'/>",
        ["package"] = "<path d='M21 8L12 3 3 8v8l9 5 9-5V8z'/><path d='M3 8l9 5 9-5M12 13v8'/>",
        ["shield"] = "<path d='M12 3l7 3v6c0 5-3.5 8-7 9-3.5-1-7-4-7-9V6l7-3z'/><path d='M9 12l2 2 4-4'/>",
        ["users"] = "<circle cx='9' cy='8' r='3.5'/><path d='M2.5 20c0-3.5 3-6 6.5-6s6.5 2.5 6.5 6'/><circle cx='17.5' cy='8.5' r='2.8'/><path d='M15.5 20c0-2.8 1.7-5 4-5.8'/>",
        ["filetext"] = "<path d='M6 2h9l5 5v15H6z'/><path d='M15 2v5h5M8.5 13h7M8.5 17h7'/>",
        ["laptop"] = "<rect x='3' y='4' width='18' height='12' rx='1.5'/><path d='M1.5 20h21'/>",
        ["search"] = "<circle cx='10.5' cy='10.5' r='6.5'/><path d='M20 20l-4.8-4.8'/>",
        ["undo"] = "<path d='M4 10h11a5 5 0 010 10h-2'/><path d='M8 5.5L3.5 10 8 14.5'/>",
        ["checkcheck"] = "<path d='M2 12l4 4L15 7'/><path d='M9 16l1.5 1.5L20 8'/>",
        ["inbox"] = "<path d='M3 12h4.5l1.5 3h6l1.5-3H21'/><path d='M5.5 5h13l2.5 7v7a1.5 1.5 0 01-1.5 1.5h-15A1.5 1.5 0 013 19v-7l2.5-7z'/>",
        ["bell"] = "<path d='M12 3a5 5 0 00-5 5v3.5c0 1-.4 2-1 2.7L5 15.5h14l-1-1.3c-.6-.7-1-1.7-1-2.7V8a5 5 0 00-5-5z'/><path d='M9.5 19a2.5 2.5 0 005 0'/>",
        ["plus"] = "<path d='M12 5v14M5 12h14'/>",
        ["upload"] = "<path d='M12 16V4M7 9l5-5 5 5'/><path d='M4 16v3a1 1 0 001 1h14a1 1 0 001-1v-3'/>",
        ["download"] = "<path d='M12 4v12M7 11l5 5 5-5'/><path d='M4 17v3a1 1 0 001 1h14a1 1 0 001-1v-3'/>",
        ["x"] = "<path d='M5 5l14 14M19 5L5 19'/>",
        ["pencil"] = "<path d='M4 20l1-4.5L16.5 4 20 7.5 8.5 19 4 20z'/><path d='M14 6.5L17.5 10'/>",
        ["userplus"] = "<circle cx='9' cy='8' r='3.5'/><path d='M2.5 20c0-3.5 3-6 6.5-6s6.5 2.5 6.5 6'/><path d='M18.5 8v5M16 10.5h5'/>",
        ["check"] = "<path d='M4 12l5 5L20 6'/>",
        ["alert"] = "<path d='M12 3l10 18H2z'/><path d='M12 9.5v5M12 17.5h.01'/>",
        ["lock"] = "<rect x='4.5' y='10.5' width='15' height='10' rx='1.5'/><path d='M8 10.5V7a4 4 0 018 0v3.5'/>",
        ["wrench"] = "<path d='M14.5 6.5a4 4 0 00-5.4 4.9L3 17.5 6.5 21l6.1-6.1a4 4 0 004.9-5.4l-2.8 2.8-2.1-2.1 2.8-2.8z'/>",
        ["cart"] = "<circle cx='9' cy='20' r='1.4'/><circle cx='18' cy='20' r='1.4'/><path d='M2.5 3h2.5l2.3 12h11l2.2-8H6'/>",
        ["truck"] = "<rect x='1.5' y='7' width='13' height='9'/><path d='M14.5 10.5H18l3 3V16h-6.5z'/><circle cx='6' cy='18.5' r='1.6'/><circle cx='17' cy='18.5' r='1.6'/>",
        ["clock"] = "<circle cx='12' cy='12' r='9'/><path d='M12 7v5.5l3.5 2'/>",
        ["boxopen"] = "<path d='M3 8l9-5 9 5v8l-9 5-9-5V8z'/><path d='M3 8l9 5 9-5'/>",
        ["helpcircle"] = "<circle cx='12' cy='12' r='9'/><path d='M9.5 9.5a2.5 2.5 0 013.9-2c1.4.8 1.3 2.6 0 3.4-.8.5-1.4 1-1.4 2M12 17h.01'/>",
        ["checkcircle"] = "<circle cx='12' cy='12' r='9'/><path d='M8 12.2l2.6 2.6L16 9.5'/>",
        ["usercheck"] = "<circle cx='9' cy='8' r='3.5'/><path d='M2.5 20c0-3.5 3-6 6.5-6s6.5 2.5 6.5 6'/><path d='M16.5 11.5l1.8 1.8 3.2-3.4'/>",
        ["archive"] = "<rect x='3' y='4' width='18' height='4' rx='1'/><path d='M5 8v11a1 1 0 001 1h12a1 1 0 001-1V8'/><path d='M10 12h4'/>",
        ["info"] = "<circle cx='12' cy='12' r='9'/><path d='M12 11v6M12 7.5h.01'/>",
        ["logout"] = "<path d='M9 21H5a1 1 0 01-1-1V4a1 1 0 011-1h4'/><path d='M16 17l5-5-5-5M21 12H9'/>",
        ["eye"] = "<path d='M1.5 12S5 5 12 5s10.5 7 10.5 7-3.5 7-10.5 7S1.5 12 1.5 12z'/><circle cx='12' cy='12' r='3'/>",
        ["tag"] = "<path d='M20.5 12.6L12.4 20.7a2 2 0 01-2.8 0l-6.3-6.3a2 2 0 010-2.8L11.4 3.5 20.5 3.5v9.1z'/><circle cx='16' cy='8' r='1.5'/>",
        ["trash"] = "<path d='M4 7h16M9 7V4.5A1.5 1.5 0 0110.5 3h3A1.5 1.5 0 0115 4.5V7M6 7l1 13.5A1.5 1.5 0 008.5 22h7a1.5 1.5 0 001.5-1.5L18 7'/>",
        ["swap"] = "<path d='M17 3l4 4-4 4'/><path d='M3 7h18'/><path d='M7 21l-4-4 4-4'/><path d='M21 17H3'/>",
        ["menu"] = "<path d='M4 7h16M4 12h16M4 17h16'/>",
    };

    public static IHtmlContent Svg(string name, int size = 16)
    {
        if (!Paths.TryGetValue(name, out var path)) return HtmlString.Empty;

        return new HtmlString(
            $"<svg class=\"icon\" width=\"{size}\" height=\"{size}\" viewBox=\"0 0 24 24\" fill=\"none\" " +
            $"stroke=\"currentColor\" stroke-width=\"1.9\" stroke-linecap=\"round\" stroke-linejoin=\"round\">{path}</svg>");
    }
}
