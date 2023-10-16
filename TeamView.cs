using System.Collections.Generic;
using System.Text;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Team View", "Strobez", "0.0.2")]
    internal class TeamView : RustPlugin
    {
        private int _maxTeamSize;

        private void OnServerInitialized()
        {
            _maxTeamSize = RelationshipManager.maxTeamSize;
        }

        [ChatCommand("team")]
        private void ShowTeamUi(BasePlayer player)
        {
            if (player.currentTeam == 0)
            {
                SendReply(player, "<color=#aaee32>[TEAM]</color> You are not in a team, you cannot use this command.");
                return;
            }

            RelationshipManager.PlayerTeam team = player.Team;

            if (team.members.Count < 8)
            {
                SendReply(player,
                    "<color=#aaee32>[TEAM]</color> You need to have at least 8 members in your team to use this command.");
                return;
            }

            string leaderName = GetTeamLeaderName(team);

            List<string> memberNames = GetTeamMemberNames(team);

            int teamSize = team.members.Count;

            CuiElementContainer container = new()
            {
                {
                    new CuiPanel
                    {
                        CursorEnabled = true,
                        Image =
                        {
                            Color = "0 0 0 0.75", Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat"
                        },
                        RectTransform =
                        {
                            AnchorMin = "0.5 0.5",
                            AnchorMax = "0.5 0.5",
                            OffsetMin = "-540 -333.897",
                            OffsetMax = "540 316.103"
                        }
                    },
                    "Overlay", "ContainerPanel"
                },
                new CuiElement
                {
                    Name = "TitleLabel",
                    Parent = "ContainerPanel",
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Text = $"{leaderName.ToUpper()}'S TEAM ({teamSize}/{_maxTeamSize})",
                            Font = "robotocondensed-bold.ttf",
                            FontSize = 28,
                            Align = TextAnchor.MiddleLeft,
                            Color = "1 1 1 1"
                        },
                        new CuiOutlineComponent {Color = "0 0 0 0.5", Distance = "1 -1"},
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 1",
                            AnchorMax = "0 1",
                            OffsetMin = "60.41 -74.078",
                            OffsetMax = "539.59 -25.922"
                        }
                    }
                },
                new CuiElement
                {
                    Name = "SubtitleLabel",
                    Parent = "ContainerPanel",
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Text = "ALL TEAM INFORMATION",
                            Font = "robotocondensed-regular.ttf",
                            FontSize = 18,
                            Align = TextAnchor.MiddleLeft,
                            Color = "1 1 1 1"
                        },
                        new CuiOutlineComponent {Color = "0 0 0 0.5", Distance = "1 -1"},
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 1",
                            AnchorMax = "0 1",
                            OffsetMin = "60.41 -109.678",
                            OffsetMax = "539.59 -74.078"
                        }
                    }
                },
                {
                    new CuiButton
                    {
                        Button = {Color = "0.7490196 0.09803922 0.1294118 1", Command = "closeteamui"},
                        Text =
                        {
                            Text = "CLOSE",
                            Font = "robotocondensed-bold.ttf",
                            FontSize = 14,
                            Align = TextAnchor.MiddleCenter,
                            Color = "1 1 1 1"
                        },
                        RectTransform =
                        {
                            AnchorMin = "0.5 0.5",
                            AnchorMax = "0.5 0.5",
                            OffsetMin = "343.833 274.08",
                            OffsetMax = "475 299.08"
                        }
                    },
                    "ContainerPanel", "CloseButton"
                },
                new CuiElement
                {
                    Name = "LeaderLabel",
                    Parent = "ContainerPanel",
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Text = "LEADER",
                            Font = "robotocondensed-bold.ttf",
                            FontSize = 18,
                            Align = TextAnchor.MiddleLeft,
                            Color = "1 1 1 1"
                        },
                        new CuiOutlineComponent {Color = "0 0 0 0.5", Distance = "1 -1"},
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0.5",
                            AnchorMax = "0 0.5",
                            OffsetMin = "60.41 162.5",
                            OffsetMax = "120.682 187.5"
                        }
                    }
                },
                new CuiElement
                {
                    Name = "LeaderName",
                    Parent = "ContainerPanel",
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Text = $"{leaderName}",
                            Font = "robotocondensed-bold.ttf",
                            FontSize = 14,
                            Align = TextAnchor.MiddleLeft,
                            Color = "0.6666667 0.9333333 0.1960784 1"
                        },
                        new CuiOutlineComponent {Color = "0 0 0 0.5", Distance = "1 -1"},
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0.5",
                            AnchorMax = "0 0.5",
                            OffsetMin = "60.414 133.7",
                            OffsetMax = "327.606 158.7"
                        }
                    }
                },
                new CuiElement
                {
                    Name = "MembersLabel",
                    Parent = "ContainerPanel",
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Text = "MEMBERS",
                            Font = "robotocondensed-bold.ttf",
                            FontSize = 18,
                            Align = TextAnchor.MiddleLeft,
                            Color = "1 1 1 1"
                        },
                        new CuiOutlineComponent {Color = "0 0 0 0.5", Distance = "1 -1"},
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0.5",
                            AnchorMax = "0 0.5",
                            OffsetMin = "60.41 87.5",
                            OffsetMax = "135.41 112.5"
                        }
                    }
                }
            };

            StringBuilder sb = new();

            for (int i = 0; i < teamSize; i++)
            {
                sb.Clear();
                int moveByX = 75 * (i / 15);

                sb.Append("Member");
                sb.Append(i);

                container.Add(new CuiElement
                {
                    Name = sb.ToString(),
                    Parent = "ContainerPanel",
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Text = $"{memberNames[i]}",
                            Font = "robotocondensed-bold.ttf",
                            FontSize = 14,
                            Align = TextAnchor.MiddleLeft,
                            Color = "0.6666667 0.9333333 0.1960784 1"
                        },
                        new CuiOutlineComponent {Color = "0 0 0 0.5", Distance = "1 -1"},
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0.5",
                            AnchorMax = "0 0.5",
                            OffsetMin = $"{60.5 + moveByX} {62.5 - 25 * (i % 15)}",
                            OffsetMax = $"{327.5 + moveByX} {87.5 - 25 * (i % 15)}"
                        }
                    }
                });
            }

            CuiHelper.DestroyUi(player, "ContainerPanel");
            CuiHelper.AddUi(player, container);
        }

        [ConsoleCommand("closeteamui")]
        private void CloseTeamUi(ConsoleSystem.Arg? arg)
        {
            BasePlayer? player = arg?.Player();

            CuiHelper.DestroyUi(player, "ContainerPanel");
        }

        private string GetTeamLeaderName(RelationshipManager.PlayerTeam team)
        {
            ulong leader = team.teamLeader;

            BasePlayer player = BasePlayer.FindByID(leader);

            return player?.displayName ?? "Unknown";
        }

        private List<string> GetTeamMemberNames(RelationshipManager.PlayerTeam team)
        {
            List<string> memberNames = new(team.members.Count);
            foreach (ulong memberId in team.members)
            {
                BasePlayer player = BasePlayer.FindByID(memberId);
                memberNames.Add(player?.displayName ?? "Unknown");
            }

            return memberNames;
        }
    }
}