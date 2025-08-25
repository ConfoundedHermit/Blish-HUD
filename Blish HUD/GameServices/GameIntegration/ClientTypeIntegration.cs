using System;
using Blish_HUD.Contexts;
using Blish_HUD.GameServices;

namespace Blish_HUD.GameIntegration {
    public sealed class ClientTypeIntegration : ServiceModule<GameIntegrationService> {

        private static readonly Logger Logger = Logger.GetLogger<ClientTypeIntegration>();

        public Gw2ClientContext.ClientType ClientType { get; private set; } = Gw2ClientContext.ClientType.Unknown;

        internal ClientTypeIntegration(GameIntegrationService service) : base(service) { /* NOOP */ }

        public override void Load() {
            GameService.Gw2Mumble.Info.BuildIdChanged += delegate { DetectClientType(); };

            var cdnInfoContext = GameService.Contexts.GetContext<CdnInfoContext>();
            if (cdnInfoContext != null) {
                cdnInfoContext.StateChanged += OnCdnInfoContextStateChanged;
            }
        }

        private void OnCdnInfoContextStateChanged(object? sender, EventArgs e) {
            if (sender is Context context && context.State == ContextState.Ready) {
                DetectClientType();
            }
        }

        private void DetectClientType() {
            var gw2ClientContext = GameService.Contexts.GetContext<Gw2ClientContext>();
            if (gw2ClientContext == null) {
                Logger.Warn("Failed to detect current Guild Wars 2 client version: Context not available");
                return;
            }

            var checkClientTypeResult = gw2ClientContext.TryGetClientType(out var contextResult);

            switch (checkClientTypeResult) {
                case ContextAvailability.Available:
                    this.ClientType = contextResult.Value;
                    Logger.Info("Detected Guild Wars 2 client to be the {clientVersionType} version.", this.ClientType);
                    break;
                case ContextAvailability.Unavailable:
                case ContextAvailability.NotReady:
                    Logger.Debug("Unable to detect current Guild Wars 2 client version: {statusForUnknown}.", contextResult.Status);
                    break;
                case ContextAvailability.Failed:
                    Logger.Warn("Failed to detect current Guild Wars 2 client version: {statusForFailed}", contextResult.Status);
                    break;
            }
        }

    }
}
