﻿using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System.Data;
using System;
using System.Text.RegularExpressions;
using static DuckBot.Services.CommonService;

namespace DuckBot.Handlers
{
    internal partial class TextMessagesHandler
    {
        private readonly Dictionary<ulong, ulong> Users = new();
        private readonly IServiceProvider _services;
        private readonly DiscordSocketClient _client;

        /// <summary>
        /// (User ID : UserMessageData)
        /// </summary>
        private readonly Dictionary<ulong, UserMessageData> _watchDog = new();

        public struct UserMessageData
        {
            public string MessageContent { get; set; }
            public int RepeatCount { get; set; }
            public int? ImageSize { get; set; }
        }

        public TextMessagesHandler(IServiceProvider services)
        {
            _services = services;
            _client = _services.GetRequiredService<DiscordSocketClient>();

            _client.MessageReceived += (message) =>
            {
                Task.Run(async () => await HandleMessageAsync(message));
                return Task.CompletedTask;
            };
        }

        private async Task HandleMessageAsync(SocketMessage sm)
        {
            Log(".");
            if (sm is not SocketUserMessage userMessage) return;
            if (userMessage.Author.IsBot || userMessage.Author.IsWebhook) return;
            if (userMessage.Author.Id == _client.CurrentUser.Id) return;

            var context = new SocketCommandContext(_client, userMessage);
            if (context.Guild is null) return;
            if (context.Channel is not SocketTextChannel textChannel) return;
            if (userMessage.Author.Id == context.Guild.OwnerId) return;
            if (userMessage.Author is not SocketGuildUser user) return;

            // Already blocked
            bool userIsBadDuckling = user.Roles.Any(r => r.Name == BAD_DUCKLING);
            if (userIsBadDuckling) return;

            // Try to block
            if (IsSpam(context))
            {
                LogRed("!");
                var badRole = context.Guild.Roles.FirstOrDefault(r => r.Name == BAD_DUCKLING);
                if (badRole is null) return;

                await user.AddRoleAsync(badRole);
                _watchDog.Remove(user.Id);

                // Delete messages
                var allChannels = context.Guild.Channels;
                await Parallel.ForEachAsync(allChannels, async (channel, ct) =>
                {
                    var allMessages = await textChannel.GetMessagesAsync().FlattenAsync();
                    await Parallel.ForEachAsync(allMessages, async (message, ct) =>
                    {
                        if (Equals(message.Author.Id, user.Id))
                            await message.DeleteAsync();
                    });
                });

                return;
            }
            else
            {
                // Start or continue tracking user level
                Users.TryAdd(user.Id, 0);
                Users[user.Id]++;
                await UpdateUserRoleAsync(user, textChannel.Guild);
            }
        }

        private async Task UpdateUserRoleAsync(SocketGuildUser user, SocketGuild guild)
        {
            ulong totalAmountOfMessages = Users[user.Id];
            
            var hatRole = guild.Roles.FirstOrDefault(r => r.Name == ROLE_HATCHLING);
            var nestRole = guild.Roles.FirstOrDefault(r => r.Name == ROLE_NESTLING);
            var fledRole = guild.Roles.FirstOrDefault(r => r.Name == ROLE_FLEDGLING);
            var grownRole = guild.Roles.FirstOrDefault(r => r.Name == ROLE_GROWNUP);
            if (new SocketRole?[4] { hatRole, nestRole, fledRole, grownRole }.Any(r => r is null)) return;

            var allRoles = new SocketRole[4] { hatRole!, nestRole!, fledRole!, grownRole! };

            if (totalAmountOfMessages <= 1)
            {
                bool hasTopRole = user.Roles.Any(userRole
                    => allRoles[0..3].Any(topRole
                        => string.Equals(userRole.Name, topRole.Name)));

                if (hasTopRole) return;
                await user.AddRoleAsync(hatRole);
            }
            else if (totalAmountOfMessages < 100 && totalAmountOfMessages >= 10)
            {
                bool hasTopRole = user.Roles.Any(userRole
                    => allRoles[1..3].Any(topRole
                        => string.Equals(userRole.Name, topRole.Name)));

                if (hasTopRole) return;
                await user.RemoveRoleAsync(hatRole);
                await user.AddRoleAsync(nestRole);
            }
            else if (totalAmountOfMessages < 1000 && totalAmountOfMessages >= 100)
            {
                bool hasTopRole = user.Roles.Any(userRole
                    => allRoles[2..3].Any(topRole
                        => string.Equals(userRole.Name, topRole.Name)));

                if (hasTopRole) return;
                await user.RemoveRoleAsync(nestRole);
                await user.AddRoleAsync(fledRole);
            }
            else if (totalAmountOfMessages >= 1000)
            {
                bool hasTopRole = user.Roles.Any(userRole
                    => string.Equals(userRole.Name, grownRole!.Name));

                if (hasTopRole) return;
                await user.RemoveRoleAsync(fledRole);
                await user.AddRoleAsync(grownRole);
            }
        }

        private bool IsSpam(SocketCommandContext context)
        {
            ulong currUserId = context.Message.Author.Id;

            // Start watching for user
            if (!_watchDog.ContainsKey(currUserId))
            {
                _watchDog.Add(currUserId, new()
                {
                    MessageContent = context.Message.Content ?? "",
                    RepeatCount = 0,
                    ImageSize = context.Message.Attachments.FirstOrDefault()?.Size
                });
                return false;
            }

            var currUser = _watchDog[currUserId];

            // Check if message is same as previous one
            bool contentIsSame = string.Equals(currUser.MessageContent, context.Message.Content);
            bool attachmentIsSame = Equals(currUser.ImageSize, context.Message.Attachments.FirstOrDefault()?.Size);

            // Not spam
            if (!(contentIsSame && attachmentIsSame))
            {
                currUser.MessageContent = context.Message.Content ?? "";
                currUser.ImageSize = context.Message.Attachments.FirstOrDefault()?.Size;
                currUser.RepeatCount = 0;
                return false;
            }
            else // spam
            {
                currUser.RepeatCount++;
                return SpamLimitIsExceeded(currUser, context);
            }
        }

        private static bool SpamLimitIsExceeded(UserMessageData currUser, SocketCommandContext context)
        {
            if (Equals(currUser.RepeatCount, 3))
            {
                Task.Run(async () => await context.Message.ReplyAsync(embed: $"{context.User.Mention} Sssh...".ToInlineEmbed(Color.Orange)));
                return false;
            }

            if (Equals(currUser.RepeatCount, 5))
            {
                
                Task.Run(async () => await context.Channel.SendMessageAsync(embed: $"{context.User.Mention} was a very, very bad duckling and *accidentally* has drown in the lake.".ToInlineEmbed(Color.Magenta)));
                return true;
            }

            return false;
        }

        [GeneratedRegex("\\<(.*?)\\>")]
        private static partial Regex MentionRegex();
    }
}
