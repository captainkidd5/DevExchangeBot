using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.SlashCommands;
#pragma warning disable 1998

namespace DevExchangeBot.Commands
{
    public class SlashRequireUserPermissions : SlashCheckBaseAttribute
    {
        // ReSharper disable once MemberCanBePrivate.Global
        public readonly Permissions Perm;

        public SlashRequireUserPermissions(Permissions perm)
            => Perm = perm;

        public override async Task<bool> ExecuteChecksAsync(InteractionContext ctx)
            => ctx.Member.Permissions.HasPermission(Perm);
    }
}
