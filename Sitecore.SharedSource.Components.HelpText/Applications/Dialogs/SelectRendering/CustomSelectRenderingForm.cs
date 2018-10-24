using Sitecore.Collections;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.Resources;
using Sitecore.Shell.Applications.Dialogs.ItemLister;
using Sitecore.Shell.Applications.Dialogs.SelectItem;
using Sitecore.Shell.Applications.Dialogs.SelectItemWithThumbnail;
using Sitecore.Shell.Controls.Splitters;
using Sitecore.Shell.DeviceSimulation;
using Sitecore.Web;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Sheer;
using Sitecore.Web.UI.WebControls;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Web.UI;

namespace Sitecore.SharedSource.Components.HelpText.Applications.Dialogs.SelectRendering
{
    [UsedImplicitly]
    public class CustomSelectRenderingForm : CustomSelectItemWithThumbnailForm
    {
        /// <summary>Renderings preview container</summary>
        protected Scrollbox Renderings;
        /// <summary>The vertical splitter</summary>
        protected VSplitterXmlControl TreeSplitter;
        /// <summary>Treeview container</summary>
        protected Scrollbox TreeviewContainer;

        /// <summary>
        /// Gets or sets a value indicating whether this instance is open properties checked.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is open properties checked; otherwise, <c>false</c>.
        /// </value>
        [UsedImplicitly]
        protected bool IsOpenPropertiesChecked
        {
            get
            {
                return (string)this.ServerProperties["IsChecked"] == "1";
            }
            set
            {
                this.ServerProperties["IsChecked"] = value ? (object)"1" : (object)"0";
            }
        }

        /// <summary>Gets or sets the open properties.</summary>
        /// <value>The open properties.</value>
        [UsedImplicitly]
        protected Checkbox OpenProperties { get; set; }

        /// <summary>Gets or sets the open properties border.</summary>
        /// <value>The open properties border.</value>
        [UsedImplicitly]
        protected Border OpenPropertiesBorder { get; set; }

        /// <summary>Gets or sets the name of the placeholder.</summary>
        /// <value>The name of the placeholder.</value>
        [UsedImplicitly]
        protected Edit PlaceholderName { get; set; }

        /// <summary>Gets or sets the placeholder name border.</summary>
        /// <value>The placeholder name border.</value>
        [UsedImplicitly]
        protected Border PlaceholderNameBorder { get; set; }

        /// <summary>Gets the filter.</summary>
        /// <param name="options">The options.</param>
        /// <returns>The filter.</returns>
        protected override string GetFilter(SelectItemOptions options)
        {
            Assert.ArgumentNotNull((object)options, nameof(options));
            if (options.IncludeTemplatesForDisplay.Count == 0 && options.ExcludeTemplatesForDisplay.Count == 0)
                return string.Empty;
            string list1 = SelectItemForm.GetList(options.IncludeTemplatesForDisplay);
            string list2 = SelectItemForm.GetList(options.ExcludeTemplatesForDisplay);
            if (options.IncludeTemplatesForDisplay.Count > 0 && options.ExcludeTemplatesForDisplay.Count > 0)
                return string.Format("(contains('{0}', ',' + @@templateid + ',') or contains('{0}', ',' + @@templatekey + ',')) and  not (contains('{1}', ',' + @@templateid + ',') or contains('{1}', ',' + @@templatekey + ','))", (object)list1, (object)list2);
            if (options.IncludeTemplatesForDisplay.Count > 0)
                return string.Format("(contains('{0}', ',' + @@templateid + ',') or contains('{0}', ',' + @@templatekey + ','))", (object)list1);
            string str1 = "{B4A0FB13-9758-427C-A7EB-1A406C045192}";
            string str2 = "{B87CD5F0-4E72-429D-90A3-B285F1D038CA}";
            string str3 = "{75D27C2B-5F88-4CC8-B1DE-8412A1628408}";
            return string.Format("not (contains('{0}', ',' + @@templateid + ',') or contains('{0}', ',' + @@templatekey + ',') or @@name='Placeholder Settings' or @@name='Devices' or @@name='Layouts' or @@id='{1}' or @@id='{2}' or @@id='{3}' or @@id='{4}')", (object)list2, (object)str1, (object)DeviceSimulationUtil.SimulatorsFolderId, (object)str2, (object)str3);
        }

        /// <summary>Defines if item is rendering</summary>
        /// <param name="item">The item</param>
        /// <returns><c>true</c> of item is a rendering item, and <c>false</c> otherwise</returns>
        protected bool IsItemRendering(Item item)
        {
            return ItemUtil.IsRenderingItem(item);
        }

        /// <summary>Defines if the item can be selected in the dialog</summary>
        /// <param name="item">The item to check</param>
        /// <returns><c>true</c> if selectable; otherwise, <c>false</c></returns>
        protected override bool IsItemSelectable(Item item)
        {
            Assert.ArgumentNotNull((object)item, nameof(item));
            return this.IsItemRendering(item);
        }

        /// <summary>Handles click on a non-rendering preview</summary>
        /// <param name="item">The non-rendering item.</param>
        protected override void OnItemClick(Item item)
        {
            Assert.ArgumentNotNull((object)item, nameof(item));
            ItemCollection children = this.DataContext.GetChildren(item);
            if (children != null && children.Count > 0)
            {
                this.Treeview.SetSelectedItem(item);
                this.Renderings.InnerHtml = this.RenderPreviews(children);
            }
            else
                SheerResponse.Alert("Please select a rendering item");
            this.SetOpenPropertiesState(item);
        }

        /// <summary>Raises the load event.</summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> instance containing the event data.</param>
        protected override void OnLoad(EventArgs e)
        {
            Assert.ArgumentNotNull((object)e, nameof(e));
            base.OnLoad(e);
            if (Context.ClientPage.IsEvent)
                return;
            this.IsOpenPropertiesChecked = Registry.GetBool("/Current_User/SelectRendering/IsOpenPropertiesChecked");
            SelectRenderingOptions renderingOptions = SelectItemOptions.Parse<SelectRenderingOptions>();
            if (renderingOptions.ShowOpenProperties)
            {
                this.OpenPropertiesBorder.Visible = true;
                this.OpenProperties.Checked = this.IsOpenPropertiesChecked;
            }
            if (renderingOptions.ShowPlaceholderName)
            {
                this.PlaceholderNameBorder.Visible = true;
                this.PlaceholderName.Value = renderingOptions.PlaceholderName;
            }
            if (!renderingOptions.ShowTree)
            {
                this.TreeviewContainer.Class = string.Empty;
                this.TreeviewContainer.Visible = false;
                this.TreeSplitter.Visible = false;
                GridPanel parent = this.TreeviewContainer.Parent as GridPanel;
                if (parent != null)
                    parent.SetExtensibleProperty((System.Web.UI.Control)this.TreeviewContainer, "class", "scDisplayNone");
                this.Renderings.InnerHtml = this.RenderPreviews((IEnumerable<Item>)renderingOptions.Items);
            }
            this.SetOpenPropertiesState(renderingOptions.SelectedItem);
        }

        /// <summary>Handles a click on the OK button.</summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        /// <remarks>When the user clicks OK, the dialog is closed by calling
        /// the <see cref="M:Sitecore.Web.UI.Sheer.ClientResponse.CloseWindow">CloseWindow</see> method.</remarks>
        /// <contract>
        ///   <requires name="sender" condition="not null" />
        ///   <requires name="args" condition="not null" />
        /// </contract>
        protected override void OnOK(object sender, EventArgs args)
        {
            Assert.ArgumentNotNull(sender, nameof(sender));
            Assert.ArgumentNotNull((object)args, nameof(args));
            if (!string.IsNullOrEmpty(this.SelectedItemId))
            {
                this.SetDialogResult(ShortID.Parse(this.SelectedItemId).ToID().ToString());
            }
            else
            {
                Item selectionItem = this.Treeview.GetSelectionItem();
                if (selectionItem != null && this.IsItemRendering(selectionItem))
                    this.SetDialogResult(selectionItem.ID.ToString());
                else
                    SheerResponse.Alert("Please select a rendering item");
            }
        }

        /// <summary>Handles click on rendering preview</summary>
        /// <param name="item">The rendering item.</param>
        protected override void OnSelectableItemClick(Item item)
        {
            Assert.ArgumentNotNull((object)item, nameof(item));
            this.SetOpenPropertiesState(item);
        }

        /// <summary>Sets the dialog result.</summary>
        /// <param name="selectedRenderingId">The selected rendering id</param>
        protected void SetDialogResult(string selectedRenderingId)
        {
            Assert.ArgumentNotNull((object)selectedRenderingId, nameof(selectedRenderingId));
            if (!this.OpenProperties.Disabled)
                Registry.SetBool("/Current_User/SelectRendering/IsOpenPropertiesChecked", this.IsOpenPropertiesChecked);
            SheerResponse.SetDialogValue(selectedRenderingId + "," + WebUtil.GetFormValue("PlaceholderName").Replace(",", "-c-") + "," + (this.OpenProperties.Checked ? "1" : "0"));
            SheerResponse.CloseWindow();
        }

        /// <summary>Sets the dialog result.</summary>
        /// <param name="selectedItem">The selected item.</param>
        protected override void SetDialogResult(Item selectedItem)
        {
            Assert.ArgumentNotNull((object)selectedItem, nameof(selectedItem));
            this.SetDialogResult(selectedItem.ID.ToString());
        }

        /// <summary>Handles the Treeview click event.</summary>
        [UsedImplicitly]
        protected void Treeview_Click()
        {
            Item selectionItem = this.Treeview.GetSelectionItem();
            if (selectionItem != null)
            {
                this.SelectedItemId = string.Empty;
                ItemCollection children = this.DataContext.GetChildren(selectionItem);
                this.Renderings.InnerHtml = children == null || children.Count <= 0 ? this.RenderEmptyPreview(selectionItem) : this.RenderPreviews(children);
            }
            this.SetOpenPropertiesState(selectionItem);
        }

        /// <summary>Renders empty preview</summary>
        /// <param name="item">The item</param>
        /// <returns>Previews markup</returns>
        private string RenderEmptyPreview(Item item)
        {
            HtmlTextWriter output = new HtmlTextWriter((TextWriter)new StringWriter());
            output.Write("<table class='scEmptyPreview'>");
            output.Write("<tbody>");
            output.Write("<tr>");
            output.Write("<td>");
            if (item == null)
                output.Write(Translate.Text("None available."));
            else if (this.IsItemRendering(item))
            {
                output.Write("<div class='scImageContainer'>");
                output.Write("<span style='height:100%; width:1px; display:inline-block;'></span>");
                string str = item.Appearance.Icon;
                int num1 = 48;
                int num2 = 48;
                if (!string.IsNullOrEmpty(item.Appearance.Thumbnail) && item.Appearance.Thumbnail != Settings.DefaultThumbnail)
                {
                    string thumbnailSrc = UIUtil.GetThumbnailSrc(item, 128, 128);
                    if (!string.IsNullOrEmpty(thumbnailSrc))
                    {
                        str = thumbnailSrc;
                        num1 = 128;
                        num2 = 128;
                    }
                }
                new ImageBuilder()
                {
                    Align = "absmiddle",
                    Src = str,
                    Width = num2,
                    Height = num1
                }.Render(output);
                output.Write("</div>");
                output.Write("<span class='scDisplayName'>");
                output.Write(item.GetUIDisplayName());
                output.Write("</span>");
            }
            else
                output.Write(Translate.Text("Please select a rendering item"));
            output.Write("</td>");
            output.Write("</tr>");
            output.Write("</tbody>");
            output.Write("</table>");
            return output.InnerWriter.ToString();
        }

        /// <summary>Renders previews</summary>
        /// <param name="items">The items</param>
        /// <returns>Previews markup</returns>
        private string RenderPreviews(IEnumerable<Item> items)
        {
            Assert.ArgumentNotNull((object)items, nameof(items));
            HtmlTextWriter output = new HtmlTextWriter((TextWriter)new StringWriter());
            bool flag = false;
            foreach (Item obj in items)
            {
                this.RenderItemPreview(obj, output);
                flag = true;
            }
            if (!flag)
                return this.RenderEmptyPreview((Item)null);
            return output.InnerWriter.ToString();
        }

        /// <summary>Renders previews</summary>
        /// <param name="items">The items</param>
        /// <returns>Previews markup</returns>
        private string RenderPreviews(ItemCollection items)
        {
            Assert.ArgumentNotNull((object)items, nameof(items));
            HtmlTextWriter output = new HtmlTextWriter((TextWriter)new StringWriter());
            foreach (Item obj in (CollectionBase)items)
                this.RenderItemPreview(obj, output);
            return output.InnerWriter.ToString();
        }

        /// <summary>Renders the help.</summary>
        /// <param name="item">The item.</param>
        private void SetOpenPropertiesState(Item item)
        {
            if (item == null || !this.IsItemRendering(item))
            {
                this.OpenProperties.Disabled = true;
                this.OpenProperties.Checked = false;
            }
            else
            {
                switch (item["Open Properties After Add"])
                {
                    case "-":
                    case "":
                        this.OpenProperties.Disabled = false;
                        this.OpenProperties.Checked = this.IsOpenPropertiesChecked;
                        break;
                    case "0":
                        if (!this.OpenProperties.Disabled)
                            this.IsOpenPropertiesChecked = this.OpenProperties.Checked;
                        this.OpenProperties.Disabled = true;
                        this.OpenProperties.Checked = false;
                        break;
                    case "1":
                        if (!this.OpenProperties.Disabled)
                            this.IsOpenPropertiesChecked = this.OpenProperties.Checked;
                        this.OpenProperties.Disabled = true;
                        this.OpenProperties.Checked = true;
                        break;
                }
            }
        }
    }
}