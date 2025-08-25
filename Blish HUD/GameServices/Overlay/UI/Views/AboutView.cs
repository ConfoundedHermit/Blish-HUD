using Blish_HUD.Graphics.UI;
using System;
using System.Diagnostics;
using Blish_HUD.Content;
using Blish_HUD.Controls;
using Blish_HUD.Overlay.UI.Presenters;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Blish_HUD.Overlay.UI.Views {
    public class AboutView : View {

        protected override void Build(Container buildPanel) {
            var backgroundTexture = AsyncTexture2D.FromAssetId(1025164);
            if (backgroundTexture != null) {
                _ = new Image(backgroundTexture) {
                    SpriteEffects = SpriteEffects.FlipHorizontally | SpriteEffects.FlipVertically,
                    Location      = new Point(buildPanel.Width - 969, buildPanel.Height - 220),
                    ClipsBounds   = false,
                    Parent        = buildPanel
                };
            }

            var infoPanel = new Panel() {
                Width  = buildPanel.Width,
                Height = 32,
                Bottom = buildPanel.Bottom,
                Parent = buildPanel
            };

            var aboutPanel = new Panel() {
                ShowBorder = true,
                Width      = buildPanel.Width,
                Height     = buildPanel.Height - infoPanel.Height,
                Parent     = buildPanel,
                CanScroll  = true,
            };

            #region "Love Message"

            var lovePanel = new Panel() {
                Size   = new Point(aboutPanel.Width - 128, 128),
                Left   = (aboutPanel.Width / 2) - ((aboutPanel.Width - 128) / 3) - 24,
                Parent = aboutPanel,
            };

            var heartTexture = AsyncTexture2D.FromAssetId(156127);
            Image? heart = null;
            if (heartTexture != null) {
                heart = new Image(heartTexture) {
                    Size     = new Point(64, 64),
                    Location = new Point(0,  lovePanel.Height / 2 - 32),
                    Parent   = lovePanel
                };
            }

            _ = new Label() {
                Font              = GameService.Content.DefaultFont16,
                Text              = Strings.GameServices.OverlayService.AboutLoveMessage,
                AutoSizeWidth     = true,
                Height            = lovePanel.Height,
                Left              = heart?.Right ?? 64,
                VerticalAlignment = VerticalAlignment.Middle,
                StrokeText        = true,
                Parent            = lovePanel
            };

            #endregion

            #region "Discord Callout"

            var discordSectionTexture = GameService.Content.GetTexture("views/about/section-splitter");
            Image discordSection;
            if (discordSectionTexture != null) {
                discordSection = new Image(discordSectionTexture!) {
                    Parent          = aboutPanel,
                    Width           = aboutPanel.Width - 64,
                    Left            = 8,
                    Height          = 16,
                    Top             = lovePanel.Bottom,
                    Opacity = 0.5f
                };
            } else {
                // Create a placeholder panel if texture is null
                discordSection = new Image() {
                    Parent          = aboutPanel,
                    Width           = aboutPanel.Width - 64,
                    Left            = 8,
                    Height          = 16,
                    Top             = lovePanel.Bottom,
                    Opacity = 0.5f
                };
            }

            var discordNote = new FormattedLabelBuilder()
                             .CreatePart(Strings.GameServices.OverlayService.About_DiscordCallToAction1, b => b.SetFontSize(ContentService.FontSize.Size16))
                             .CreatePart(Strings.GameServices.OverlayService.About_DiscordCallToAction2, b => b.SetFontSize(ContentService.FontSize.Size16).SetTextColor(Color.FromNonPremultiplied(88, 101, 242, 255)).MakeBold())
                             .CreatePart(Strings.GameServices.OverlayService.About_DiscordCallToAction3, b => b.SetFontSize(ContentService.FontSize.Size16))
                             .SetWidth((int)(discordSection.Width * 0.5f) - 48)
                             .AutoSizeHeight()
                             .SetHorizontalAlignment(HorizontalAlignment.Center)
                             .Wrap()
                             .Build();

            discordNote.Top    = discordSection.Bottom + 8;
            discordNote.Left   = discordSection.Left + 48;
            discordNote.Parent = aboutPanel;

            var callToAction = new Label() {
                Font                = GameService.Content.DefaultFont16,
                Text                = Strings.GameServices.OverlayService.About_DiscordCallToAction_Question,
                Top                 = discordNote.Top - 4,
                Width               = discordNote.Width,
                AutoSizeHeight      = true,
                Left                = discordNote.Right + 32,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment   = VerticalAlignment.Top,
                StrokeText          = true,
                Parent              = aboutPanel
            };

            var discordBttn = new StandardButton() {
                Text   = Strings.GameServices.OverlayService.About_DiscordCallToAction_Button,
                Left   = callToAction.Left + callToAction.Width / 2 - 85,
                Top    = callToAction.Bottom,
                Width  = 170,
                Parent = aboutPanel
            };

            discordBttn.Click += (s, e) => Process.Start("https://link.blishhud.com/discord");

            var bottomDiscordSectionTexture = GameService.Content.GetTexture("views/about/section-splitter");
            Image bottomDiscordSection;
            if (bottomDiscordSectionTexture != null) {
                bottomDiscordSection = new Image(bottomDiscordSectionTexture!) {
                    Parent  = aboutPanel,
                    Width   = aboutPanel.Width - 64,
                    Left    = 8,
                    Height  = 16,
                    Top     = discordNote.Bottom + 8,
                    Opacity = 0.5f
                };
            } else {
                // Create a placeholder if texture is null
                bottomDiscordSection = new Image() {
                    Parent  = aboutPanel,
                    Width   = aboutPanel.Width - 64,
                    Left    = 8,
                    Height  = 16,
                    Top     = discordNote.Bottom + 8,
                    Opacity = 0.5f
                };
            }

            int fadeTop    = discordSection.Top       + discordSection.Height       / 2;
            int fadeBottom = bottomDiscordSection.Top + bottomDiscordSection.Height / 2;

            var leftFadeTexture = AsyncTexture2D.FromAssetId(156044);
            Image? leftFade = null;
            if (leftFadeTexture != null) {
                leftFade = new Image(leftFadeTexture) {
                    Width         = bottomDiscordSection.Width / 2,
                    Left          = bottomDiscordSection.Left,
                    Top           = fadeTop,
                    ZIndex        = -1,
                    Height        = fadeBottom - fadeTop,
                    Parent        = aboutPanel,
                    SpriteEffects = SpriteEffects.FlipHorizontally,
                    Opacity       = 0.4f
                };
            }

            var rightFadeTexture = AsyncTexture2D.FromAssetId(156044);
            if (rightFadeTexture != null) {
                _ = new Image(rightFadeTexture) {
                    Width   = bottomDiscordSection.Width / 2,
                    Left    = leftFade?.Right ?? bottomDiscordSection.Left + bottomDiscordSection.Width / 2,
                    Top     = fadeTop,
                    ZIndex  = -1,
                    Height  = fadeBottom - fadeTop,
                    Parent  = aboutPanel,
                    Opacity = 0.4f
                };
            }

            #endregion

            #region "Available Updates"

            var update = GameService.Overlay.OverlayUpdateHandler.GetUpdateAvailable();

            if (update.Available) {
                _ = new Label() {
                    Parent = aboutPanel,
                    Top = bottomDiscordSection.Bottom,
                    Text = $"An update to Blish HUD v{update.NewManifest.Version} is available!",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Middle,
                    Left = 0,
                    TextColor = Color.Orange,
                    Width = aboutPanel.Width,
                    Height = 50
                };

                var updateButton = new StandardButton() {
                    Text = $"Update Blish HUD",
                    Width = 164,
                    Parent = aboutPanel
                };
                updateButton.Location = new Point(
                    aboutPanel.Width / 2 - updateButton.Width / 2,
                    bottomDiscordSection.Bottom + 45
                );

                updateButton.Click += (s, e) => {
                    GameService.Overlay.OverlayUpdateHandler.ShowReleaseSplash(update.NewManifest, false);
                };
            } else {
                _ = new Label() {
                    Parent = aboutPanel,
                    Top = bottomDiscordSection.Bottom,
                    Text = "You are running the latest version of Blish HUD.",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Middle,
                    Left = 0,
                    Width = aboutPanel.Width,
                    Height = 90
                };
            }

            var finalSectionTexture = GameService.Content.GetTexture("views/about/section-splitter");
            if (finalSectionTexture != null) {
                _ = new Image(finalSectionTexture!) {
                    Parent = aboutPanel,
                    Width = aboutPanel.Width - 64,
                    Left = 8,
                    Height = 16,
                    Top = fadeBottom + 90,
                    Opacity = 0.25f
                };
            }

            #endregion

            var gw2CopyrightStatement = new Label() {
                Font                = GameService.Content.DefaultFont16,
                Text                = string.Format(Strings.GameServices.OverlayService.AboutAnetNotice, DateTime.Now.Year),
                AutoSizeHeight      = true,
                Width               = aboutPanel.Width - 24,
                StrokeText          = true,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment   = VerticalAlignment.Top,
                Parent              = aboutPanel
            };

            gw2CopyrightStatement.Bottom = aboutPanel.Height - 48;

            #region "Software Licenses"

            var thirdPartySoftwareHeading = new Label() {
                Font                = GameService.Content.DefaultFont16,
                Text                = Strings.GameServices.OverlayService.AboutThirdPartySoftwareHeading,
                Top                 = gw2CopyrightStatement.Bottom + 64,
                Width               = aboutPanel.Width,
                StrokeText          = true,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment   = VerticalAlignment.Top,
                Parent              = aboutPanel,
            };

            var thirdPartySoftwareStatement = new Label() {
                Font                = GameService.Content.DefaultFont14,
                Text                = Strings.GameServices.OverlayService.AboutThirdPartySoftware,
                AutoSizeHeight      = true,
                Top                 = thirdPartySoftwareHeading.Bottom + 32,
                Width               = aboutPanel.Width,
                Left                = 24,
                StrokeText          = false,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment   = VerticalAlignment.Top,
                Parent              = aboutPanel,
            };

            #endregion

            var version = new Label() {
                AutoSizeHeight      = true,
                Width               = infoPanel.Width,
                Text                = $"{Strings.Common.BlishHUD} v{Program.OverlayVersion.BaseAndPrerelease()}",
                BasicTooltipText    = $"v{Program.OverlayVersion}",
                Font                = GameService.Content.DefaultFont14,
                HorizontalAlignment = HorizontalAlignment.Right,
                StrokeText          = true,
                ClipsBounds         = false,
                Parent              = infoPanel
            };

            var mumbleConnection = new ViewContainer() {
                Size   = new Point(128, 20),
                Left   = 8,
                Parent = infoPanel
            };

            var arcdpsBridgeConnection = new ViewContainer() {
                Size   = new Point(128, 20),
                Left   = mumbleConnection.Right,
                Parent = infoPanel
            };

            var mumbleView = new ConnectionStatusView().WithPresenter(new ConnectionStatusPresenter(() => Strings.GameServices.OverlayService.ConnectionStatus_Mumble_Name,
                                                                                                   () => GameService.Gw2Mumble.IsAvailable,
                                                                                                   () => GameService.Gw2Mumble.IsAvailable
                                                                                                             ? string.Format(Strings.GameServices.OverlayService.ConnectionStatus_Mumble_Connected, GameService.Gw2Mumble.CurrentMumbleMapName)
                                                                                                             : string.Format(Strings.GameServices.OverlayService.ConnectionStatus_Mumble_Disconnected, GameService.Gw2Mumble.CurrentMumbleMapName)));
            if (mumbleView != null) {
                mumbleConnection.Show(mumbleView);
            }

            var arcdpsView = new ConnectionStatusView().WithPresenter(new ConnectionStatusPresenter(() => Strings.GameServices.OverlayService.ConnectionStatus_ArcDPSBridge_Name,
                                                                                                   () => GameService.ArcDps.Running,
                                                                                                   () => GameService.ArcDps.Running
                                                                                                             ? Strings.GameServices.OverlayService.ConnectionStatus_ArcDPSBridge_Connected
                                                                                                             : Strings.GameServices.OverlayService.ConnectionStatus_ArcDPSBridge_Disconnected));
            if (arcdpsView != null) {
                arcdpsBridgeConnection.Show(arcdpsView);
            }
        }
    }
}
