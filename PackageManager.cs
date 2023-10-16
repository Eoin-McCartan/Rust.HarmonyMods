using System;
using System.Collections.Generic;
using Facepunch.Extend;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Plugins
{
    [Info("PackageManager", "Strobez", "0.0.1")]
    public class PackageManager : RustPlugin
    {
        #region Data Classes
        public readonly List<string> _validPackages = new List<string> { "vip", "vip+" };
        public readonly Dictionary<string, UserPackages> _users = new Dictionary<string, UserPackages>();

        public class UserPackages
        {
            public Dictionary<string, PackageInformation>? Packages { get; init; }
        }

        public class PackageInformation
        {
            public DateTime ExpireDate { get; set; }
            public string? Group { get; init; }
        }
        #endregion

        #region Package Manager
        [Command("package")]
        private void PackageCmd(ConsoleSystem.Arg arg)
        {
            if (!arg.IsRcon) return;

            string[] args = arg.Args;

            if (args.Length < 2)
            {
                arg.ReplyWith("Usage: package <add|remove|show>");
                return;
            }

            if (!args[2].IsSteamId())
            {
                arg.ReplyWith("Invalid SteamID");
                return;
            }

            switch (args[1])
            {
                case "add":
                    if (args.Length < 3)
                    {
                        arg.ReplyWith("Usage: package add <steamid> <package name>");
                        return;
                    }

                    if (!_validPackages.Contains(args[3]))
                    {
                        arg.ReplyWith("Invalid Package");
                        return;
                    }

                    PackageAdd(arg, args[2], args[3], args[4]);
                    break;
                case "remove":
                    if (args.Length < 3)
                    {
                        arg.ReplyWith("Usage: package remove <steamid> <package name>");
                        return;
                    }

                    if (!_validPackages.Contains(args[3]))
                    {
                        arg.ReplyWith("Invalid Package");
                        return;
                    }

                    PackageRemove(arg, args[2], args[3]);
                    break;
                case "show":
                    PackageStatus(arg, args[2]);
                    break;
                default:
                    arg.ReplyWith("Usage: package <add|remove|show>");
                    break;
            }
        }

        private void PackageStatus(ConsoleSystem.Arg arg, string steamid)
        {
            if (!_users.ContainsKey(steamid))
            {
                arg.ReplyWith($"{steamid} has no packages.");
                return;
            }

            string packages = string.Empty;

            foreach (KeyValuePair<string, PackageInformation> package in _users[steamid].Packages)
            {
                packages +=
                    $"{package.Key}: {package.Value.ExpireDate} ({(package.Value.ExpireDate > DateTime.Now.AddDays(30) ? "Expired" : "Active")})\n";
            }

            string msg = $"{steamid} has the following packages:\n\n{packages}";
            
            arg.ReplyWith(msg);
        }

        private void PackageAdd(ConsoleSystem.Arg arg, string steamid, string package, string forced)
        {
            bool isForced = forced == "true" || forced == "false";

            if (_users.ContainsKey(steamid) && _users[steamid].Packages.ContainsKey(package) && !isForced)
            {
                arg.ReplyWith($"Failed to add package {package} to {steamid}! If they already have the package you can force it with \"true\" added onto the end of the command which will reset expiry.");
                return;
            }

            UserPackages user = new UserPackages
            {
                Packages = new Dictionary<string, PackageInformation>()
            };

            if (_users.ContainsKey(steamid) && _users[steamid].Packages.ContainsKey(package))
            {
                _users[steamid].Packages[package].ExpireDate = DateTime.Now.AddDays(30);
            }
            else
            {
                user.Packages.Add(package, new PackageInformation
                {
                    ExpireDate = DateTime.Now.AddDays(30),
                    Group = package
                });

                _users.Add(steamid, user);
            }
            
            permission.AddUserGroup(steamid, package);

            arg.ReplyWith($"{steamid} was granted package {package} until {DateTime.Now.AddDays(30)}");
        }



        private void PackageRemove(ConsoleSystem.Arg arg, string steamid, string package)
        {
            if (!_users.ContainsKey(steamid) || !_users[steamid].Packages.ContainsKey(package))
            {
                arg.ReplyWith($"Failed to remove package {package} from {steamid}! They don't have the package.");
                return;
            }

            _users[steamid].Packages.Remove(package);

            permission.RemoveUserGroup(steamid, package);

            arg.ReplyWith($"{steamid} was removed from package {package}");
        }
        #endregion

        #region Oxide Hooks
        private void OnPlayerConnected(IPlayer player)
        {
            if (!_users.ContainsKey(player.Id)) return;

            foreach (KeyValuePair<string, PackageInformation> package in _users[player.Id].Packages)
            {
                if (package.Value.ExpireDate < DateTime.Now)
                {
                    _users[player.Id].Packages.Remove(package.Key);
                    player.RemoveFromGroup(package.Value.Group);
                    continue;
                }

                if (!player.BelongsToGroup(package.Value.Group))
                {
                    player.AddToGroup(package.Value.Group);
                }
            }
        }

        private void OnPlayerDisconnected(IPlayer player)
        {
            if (!_users.ContainsKey(player.Id)) return;

            foreach (KeyValuePair<string, PackageInformation> package in _users[player.Id].Packages)
            {
                if (package.Value.ExpireDate < DateTime.Now)
                {
                    _users[player.Id].Packages.Remove(package.Key);
                    player.RemoveFromGroup(package.Value.Group);
                    continue;
                }

                if (!player.BelongsToGroup(package.Value.Group))
                {
                    player.AddToGroup(package.Value.Group);
                }
            }
        }
        #endregion
    }
}
