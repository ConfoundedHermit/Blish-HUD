using System;
using System.Threading.Tasks;
using Blish_HUD.Controls;
using Blish_HUD.Graphics.UI;
using Blish_HUD.Settings.UI.Views;

namespace Blish_HUD.Settings.UI.Presenters {

    public class SettingsMenuPresenter : Presenter<SettingsMenuView, ISettingsMenuRegistrar> {

        public SettingsMenuPresenter(SettingsMenuView view, ISettingsMenuRegistrar model) : base(view, model) { }

        protected override Task<bool> Load(IProgress<string> progress) {
            this.View.MenuItemSelected += OnMenuItemSelected;

            this.Model.RegistrarListChanged += OnRegistrarListChanged;

            return base.Load(progress);
        }

        private void OnRegistrarListChanged(object sender, EventArgs e) => UpdateView();

        private void OnMenuItemSelected(object sender, ControlActivatedEventArgs e) {
            var menuItem = e.ActivatedControl as MenuItem;
            if (menuItem != null) {
                var view = this.Model.GetMenuItemView(menuItem);
                if (view != null) {
                    this.View.SetSettingView(view);
                }
            }
        }

        protected override void UpdateView() {
            this.View.SetMenuItems(this.Model.GetSettingMenus());
        }

    }
}
