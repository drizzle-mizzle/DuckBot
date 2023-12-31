﻿using Discord;

namespace DuckBot.Services
{
    internal static partial class CommonService
    {
        internal static readonly string WARN_SIGN_UNICODE = "⚠";
        internal static readonly string WARN_SIGN_DISCORD = ":warning:";
        internal static readonly string OK_SIGN_DISCORD = ":white_check_mark: ";

        internal static Emoji DUCK_EMOJI = new("\uD83E\uDD86");
        internal static Emoji RADIO_EMOJI = new("\uD83D\uDCFB");
        internal static Emoji KEYBOARD_EMOJI = new("\u2328\uFE0F");

        internal static string ROLE_DUCKLINGS = "ducklings";
        internal static string ROLE_HATCHLING = "hatchling";
        internal static string ROLE_NESTLING = "nestling";
        internal static string ROLE_FLEDGLING = "fledgling";
        internal static string ROLE_GROWNUP = "grown-up duckling";
        internal static string BAD_DUCKLING = "bad duckling";

        internal static string ROLE_SUB = "CharacterEngine sub";
        internal static string ROLE_SELFHOSTED = "Self-hosted";

        internal static List<string> ALL_ROLES = new() { ROLE_DUCKLINGS, ROLE_HATCHLING, ROLE_NESTLING, ROLE_FLEDGLING, ROLE_GROWNUP, BAD_DUCKLING, ROLE_SELFHOSTED, ROLE_SUB };
        internal static Dictionary<string, string> FREE_ROLES = new()
        {
            { DUCK_EMOJI.Name, ROLE_DUCKLINGS },
            { RADIO_EMOJI.Name, ROLE_SUB },
            { KEYBOARD_EMOJI.Name, ROLE_SELFHOSTED }
        };
    }
}
