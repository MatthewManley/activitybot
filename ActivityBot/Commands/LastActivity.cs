using Discord;
using Discord.WebSocket;
using Domain.Repos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ActivityBot.Commands
{
    public class LastActivity : ISocketSlashCommandHandler
    {
        private readonly IActivityRepo activityRepo;

        public LastActivity(IActivityRepo activityRepo)
        {
            this.activityRepo = activityRepo;
        }

        public async Task Interact(SocketSlashCommand slashCommand)
        {
            if (slashCommand.User is not SocketGuildUser guildUser)
            {
                await slashCommand.RespondAsync("This command can only be run in a guild", ephemeral: true);
                return;
            }

            if (!guildUser.GuildPermissions.Administrator && guildUser.Id != 107649869665046528)
            {
                await slashCommand.RespondAsync("You must have administrator permission to run this command", ephemeral: true);
                return;
            }

            var user = slashCommand.Data.Options.FirstOrDefault();
            if (user is null || user.Type != Discord.ApplicationCommandOptionType.User)
            {
                throw new Exception();
            }
            var cmdGuildUser = (IGuildUser)user.Value;
            if (cmdGuildUser is null)
                throw new Exception("Error parsing userid");

            var activity = await activityRepo.Get(guildUser.Guild.Id, cmdGuildUser.Id);

            if (activity == null)
            {
                await slashCommand.RespondAsync("No recorded activity for that user", ephemeral: true);
                return;
            }

            var dt = activity.LastActivity;
            var minute = dt.Minute - dt.Minute % 10;
            var stripped = new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, minute, 0, DateTimeKind.Utc);
            var tag = Discord.TimestampTag.FromDateTime(stripped, Discord.TimestampTagStyles.ShortDateTime);
            await slashCommand.RespondAsync($"Last Activity for {cmdGuildUser.Mention}: {tag}\nNote this time is not exact.", allowedMentions: Discord.AllowedMentions.None, ephemeral: true);
        }
    }
}
