using System.Web;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.Pipelines.RenderItemTile;
using Sitecore.Shell.Applications.Dialogs.SelectItem;
using Sitecore.Shell.Web.UI;
using Sitecore.Web.UI;
using Sitecore.Web.UI.Sheer;
using System.Web.UI;
using Sitecore.Configuration;
using Sitecore.StringExtensions;

namespace Sitecore.SharedSource.Components.HelpText.Applications.Dialogs.SelectRendering
{
    public abstract class CustomSelectItemWithThumbnailForm : SelectItemForm
    {
        /// <summary>The CSS class for thumbnails</summary>
        protected const string thumbnailClassName = "scItemThumbnail";

        /// <summary>Gets or sets the selected item short Id.</summary>
        /// <value>The selected item.</value>
        public string SelectedItemId
        {
            get
            {
                return StringUtil.GetString(this.ServerProperties[nameof(SelectedItemId)]);
            }
            set
            {
                Assert.ArgumentNotNull((object)value, nameof(value));
                this.ServerProperties[nameof(SelectedItemId)] = (object)value;
            }
        }

        /// <summary>Defines if the item can be selected in the dialog</summary>
        /// <param name="item">The item to check</param>
        /// <returns>
        /// <c>true</c> if selectable; otherwise, <c>false</c>
        /// </returns>
        protected abstract bool IsItemSelectable(Item item);

        /// <summary>Handles click on non-selectable item</summary>
        /// <param name="id">The ID of the item</param>
        /// <param name="language">The language</param>
        /// <param name="version">The version</param>
        [UsedImplicitly]
        protected virtual void ItemPreview_Click(string id, string language, string version)
        {
            Assert.ArgumentNotNull((object)id, nameof(id));
            Assert.ArgumentNotNull((object)language, nameof(language));
            Assert.ArgumentNotNull((object)version, nameof(version));
            Item obj = Context.ContentDatabase.GetItem(id, Language.Parse(language), Version.Parse(version));
            if (obj != null)
            {
                if (!string.IsNullOrEmpty(this.SelectedItemId))
                    SheerResponse.SetAttribute(string.Format("I{0}", (object)this.SelectedItemId), "class", "scItemThumbnail");
                this.SelectedItemId = string.Empty;
            }
            this.OnItemClick(obj);
        }

        /// <summary>Handles click on selectable item</summary>
        /// <param name="id">The ID of the item</param>
        /// <param name="language">The language</param>
        /// <param name="version">The version</param>
        [UsedImplicitly]
        protected virtual void SelectableItemPreview_Click(string id, string language, string version)
        {
            Assert.ArgumentNotNull((object)id, nameof(id));
            Assert.ArgumentNotNull((object)language, nameof(language));
            Assert.ArgumentNotNull((object)version, nameof(version));
            Item obj = Context.ContentDatabase.GetItem(id, Language.Parse(language), Version.Parse(version));
            if (obj != null)
            {
                if (!string.IsNullOrEmpty(this.SelectedItemId))
                    SheerResponse.SetAttribute(string.Format("I{0}", (object)this.SelectedItemId), "class", "scItemThumbnail");
                this.SelectedItemId = obj.ID.ToShortID().ToString();
                SheerResponse.SetAttribute(string.Format("I{0}", (object)this.SelectedItemId), "class", "scItemThumbnailSelected");
            }
            this.OnSelectableItemClick(obj);
        }

        /// <summary>Handles double click on selectable item</summary>
        /// <param name="id">The ID of the item</param>
        /// <param name="language">The language</param>
        /// <param name="version">The version</param>
        [UsedImplicitly]
        protected virtual void SelectableItemPreview_DblClick(string id, string language, string version)
        {
            Assert.ArgumentNotNull((object)id, nameof(id));
            Assert.ArgumentNotNull((object)language, nameof(language));
            Assert.ArgumentNotNull((object)version, nameof(version));
            Item selectedItem = Context.ContentDatabase.GetItem(id, Language.Parse(language), Version.Parse(version));
            if (selectedItem == null)
                return;
            this.SetDialogResult(selectedItem);
        }

        /// <summary>
        /// Derived types specific logic for clcicking on non-selectable item
        /// </summary>
        /// <param name="item">The non-selectable item</param>
        protected virtual void OnItemClick(Item item)
        {
        }

        /// <summary>
        /// Derived types specific logic for clcicking on selectable item
        /// </summary>
        /// <param name="item">The selectable item</param>
        protected virtual void OnSelectableItemClick(Item item)
        {
        }

        /// <summary>Renders preview for the item</summary>
        /// <param name="item">The item to render preview for</param>
        /// <param name="output">The html text writer</param>
        protected void RenderItemPreview(Item item, System.Web.UI.HtmlTextWriter output)
        {
            string click = string.Empty;
            string doubleClick = string.Empty;
            if (this.IsItemSelectable(item))
            {
                click = StringUtil.EscapeJavascriptString(string.Format("SelectableItemPreview_Click(\"{0}\",\"{1}\",\"{2}\")", item.ID, item.Language, item.Version));
                doubleClick = StringUtil.EscapeJavascriptString(string.Format("SelectableItemPreview_DblClick(\"{0}\",\"{1}\",\"{2}\")", item.ID, item.Language, item.Version));
            }
            else
            {
                click = StringUtil.EscapeJavascriptString(string.Format("ItemPreview_Click(\"{0}\",\"{1}\",\"{2}\")", item.ID, item.Language, item.Version));
            }
            var renderingThumbnail = ItemTileService.RenderTile(
                new RenderItemTileArgs(item, TileView.Thumbnails, ImageDimension.idDefault, click)
                {
                    DoubleClick = doubleClick
                });
            var helptextContainerHtml = HttpUtility.HtmlDecode(Settings.GetSetting("Component.Helptext.Container"));
            var enableRenderingHelptext = HttpUtility.HtmlDecode(Settings.GetSetting("Component.Helptext.enable")) == "true";

            if (!helptextContainerHtml.IsNullOrEmpty() && enableRenderingHelptext)
            {
                var longHelpText = "";
                var shortHelpText = "";
                var linkHelpUrl = "";
                var helptextFieldLongDescription = Settings.GetSetting("Component.Helptext.Field.LongDescription");
                var helptextFieldShortDescription = Settings.GetSetting("Component.Helptext.Field.ShortDescription");
                var helptextFieldHelpLink = Settings.GetSetting("Component.Helptext.Field.HelpLink");

                if (helptextFieldLongDescription != null && !item.Fields[helptextFieldLongDescription].Value.IsNullOrEmpty())
                {
                    longHelpText = item.Fields[helptextFieldLongDescription].Value;
                }

                if (helptextFieldShortDescription != null && !item.Fields[helptextFieldShortDescription].Value.IsNullOrEmpty())
                {
                    shortHelpText = item.Fields[helptextFieldShortDescription].Value;
                }

                if (helptextFieldHelpLink != null && !item.Fields[helptextFieldHelpLink].Value.IsNullOrEmpty())
                {

                    Sitecore.Data.Fields.LinkField helpLinkField = item.Fields[helptextFieldHelpLink];
                    if (helpLinkField != null && !helpLinkField.GetFriendlyUrl().IsNullOrEmpty())
                    {
                        var helpLinkHtml = Settings.GetSetting("Component.Helptext.HelpLink.html");
                        linkHelpUrl = helpLinkHtml.Replace("#HELPTEXTLINKURL", helpLinkField.GetFriendlyUrl());
                    }
                }

                if (!linkHelpUrl.IsNullOrEmpty() || !longHelpText.IsNullOrEmpty())
                {
                    var newRenderingThumbnail = helptextContainerHtml.Replace("#HELPTEXTLINKURLPLACEHOLDER", linkHelpUrl).Replace("#HELPTEXTSHORTDESCRIPTION", shortHelpText).Replace("#HELPTEXTLONGDESCRIPTION", longHelpText).Replace("#HELPTEXTRENDERING", renderingThumbnail);
                    output.Write(newRenderingThumbnail);
                }
                else
                {
                    output.Write(renderingThumbnail);
                }
            }
            else
            {
                output.Write(renderingThumbnail);
            }
        }
    }
}
