using System;
using CodeBucket.Bitbucket.Controllers;
using BitbucketSharp.Models;
using System.Collections.Generic;
using MonoTouch.Dialog;
using MonoTouch.UIKit;
using System.Drawing;
using MonoTouch.Foundation;
using System.Linq;
using MonoTouch;
using CodeBucket.Controllers;
using CodeFramework.Controllers;
using CodeFramework.Views;
using CodeFramework.Elements;

namespace CodeBucket.Bitbucket.Controllers.Issues
{
    public class InternalIssueInfoModel
    {
        public IssueModel Issue { get; set; }
        public List<CommentModel> Comments { get; set; }
    }

    public class IssueInfoController : BaseModelDrivenController
    {
        public int Id { get; private set; }
        public string User { get; private set; }
        public string Slug { get; private set; }
        public Action<IssueModel> ModelChanged;

        private readonly HeaderView _header;
        private readonly Section _comments, _details;
        private readonly MultilinedElement _desc;
        private readonly SplitElement _split1, _split2, _split3;
        private readonly StyledElement _responsible;

        private bool _scrollToLastComment;
        private bool _issueRemoved;

        public IssueInfoController(string user, string slug, int id)
            : base(typeof(InternalIssueInfoModel))
        {
            User = user;
            Slug = slug;
            Id = id;
            Title = "Issue #" + id;

            NavigationItem.RightBarButtonItem = new UIBarButtonItem(NavigationButton.Create(CodeFramework.Images.Buttons.Edit, () => {
                var m = Model as InternalIssueInfoModel;
                var editController = new IssueEditController {
                     ExistingIssue = m.Issue,
                     Username = User,
                     RepoSlug = Slug,
                     Title = "Edit Issue",
                     Success = EditingComplete,
                 };
                NavigationController.PushViewController(editController, true);
            }));
            NavigationItem.RightBarButtonItem.Enabled = false;

            Style = UITableViewStyle.Grouped;
            Root.UnevenRows = true;
            _header = new HeaderView(View.Bounds.Width) { ShadowImage = false };
            Root.Add(new Section(_header));

            _desc = new MultilinedElement("") { BackgroundColor = UIColor.White };
            _desc.CaptionFont = _desc.ValueFont;
            _desc.CaptionColor = _desc.ValueColor;

            _split1 = new SplitElement(new SplitElement.Row { Image1 = Images.Buttons.Cog, Image2 = Images.Priority }) { BackgroundColor = UIColor.White };
            _split2 = new SplitElement(new SplitElement.Row { Image1 = Images.Buttons.Flag, Image2 = Images.ServerComponents }) { BackgroundColor = UIColor.White };
            _split3 = new SplitElement(new SplitElement.Row { Image1 = Images.SitemapColor, Image2 = Images.Milestone }) { BackgroundColor = UIColor.White };

            _responsible = new StyledElement("Unassigned", Images.Buttons.Person)
            {
                Font = StyledElement.DefaultDetailFont,
                TextColor = StyledElement.DefaultDetailColor,
            };
            _responsible.Tapped += () =>
            {
                var m = Model as InternalIssueInfoModel;
                if (m != null && m.Issue.Responsible != null)
                    NavigationController.PushViewController(new ProfileController(m.Issue.Responsible.Username), true);
            };

            var addComment = new StyledElement("Add Comment", Images.Pencil);
            addComment.Tapped += AddCommentTapped;

            _comments = new Section();
            _details = new Section { _split1, _split2, _split3, _responsible };

            Root.Add(_details);
            Root.Add(_comments);
            Root.Add(new Section { addComment });
        }

        void EditingComplete(IssueModel model)
        {
            if (ModelChanged != null)
                ModelChanged(model);

            //If it's null then we've deleted it!
            if (model == null)
            {
                _issueRemoved = true;
            }
            //Otherwise let's just reassign the model and call the OnRefresh to update the screen!
            else
            {
                var m = Model as InternalIssueInfoModel;
                m.Issue = model;
                Render();
            }
        }

        public override void ViewDidAppear(bool animated)
        {
            base.ViewDidAppear(animated);
            if (_issueRemoved)
            {
                NavigationController.PopViewControllerAnimated(true);
            }
        }

        void AddCommentTapped()
        {
            var composer = new Composer();
            composer.NewComment(this, () =>
            {
                var comment = new CommentModel { Content = composer.Text };

                composer.DoWork(() =>
                {
                    Application.Client.Users[User].Repositories[Slug].Issues[Id].Comments.Create(comment);

                    InvokeOnMainThread(() =>
                    {
                        composer.CloseComposer();
                        _scrollToLastComment = true;
                        Model = null;
                        UpdateAndRender();
                    });
                }, ex =>
                {
                    Utilities.ShowAlert("Unable to post comment!", ex.Message);
                    composer.EnableSendButton = true;
                });
            });
        }

        protected override void OnRender()
        {
            BeginInvokeOnMainThread(() => { NavigationItem.RightBarButtonItem.Enabled = true; });
            var model = Model as InternalIssueInfoModel;
            _header.Title = model.Issue.Title;
            _header.Subtitle = "Updated " + (model.Issue.UtcLastUpdated).ToDaysAgo();
            _split1.Value.Text1 = model.Issue.Status;
            _split1.Value.Text2 = model.Issue.Priority;
            _split2.Value.Text1 = model.Issue.Metadata.Kind;
            _split2.Value.Text2 = model.Issue.Metadata.Component ?? "No Component";
            _split3.Value.Text1 = model.Issue.Metadata.Version ?? "No Version";
            _split3.Value.Text2 = model.Issue.Metadata.Milestone ?? "No Milestone";
            _desc.Caption = model.Issue.Content;
            _responsible.Caption = model.Issue.Responsible != null ? model.Issue.Responsible.Username : "Unassigned";
            if (model.Issue.Responsible != null)
                _responsible.Accessory = UITableViewCellAccessory.DisclosureIndicator;

            if (!string.IsNullOrEmpty(_desc.Caption))
            {
                _desc.Caption = _desc.Caption.Trim();
                if (_desc.Parent == null)
                {
                    InvokeOnMainThread(() => _details.Insert(0, _desc));
                }
            }

            var comments = new List<Element>(model.Comments.Count);
            model.Comments.OrderBy(x => (x.UtcCreatedOn)).ToList().ForEach(x =>
            {
                if (!string.IsNullOrEmpty(x.Content))
                    comments.Add(new CommentElement
                                     {
                                         Name = x.AuthorInfo.Username,
                                         Time = (x.UtcCreatedOn),
                                         String = x.Content,
                                         Image = CodeFramework.Images.Misc.Anonymous,
                                         ImageUri = new Uri(x.AuthorInfo.Avatar),
                                         BackgroundColor = UIColor.White,
                                     });
            });


            InvokeOnMainThread(delegate
            {
                _header.SetNeedsDisplay();
                ReloadData();
                _comments.Clear();
                _comments.Insert(0, UITableViewRowAnimation.None, comments);

                if (_scrollToLastComment && _comments.Elements.Count > 0)
                {
                    TableView.ScrollToRow(NSIndexPath.FromRowSection(_comments.Elements.Count - 1, 2), UITableViewScrollPosition.Top, true);
                    _scrollToLastComment = false;
                }
            });
        }

        protected override object OnUpdateModel(bool forced)
        {
            var l = Application.Client.Users[User].Repositories[Slug].Issues[Id];
            var m = new InternalIssueInfoModel
                        {
                            Comments = l.Comments.GetComments(forced),
                            Issue = l.GetIssue(forced),
                        };
            return m;
        }

        public override UIView InputAccessoryView
        {
            get
            {
                var u = new UIView(new RectangleF(0, 0, 320f, 27)) { BackgroundColor = UIColor.White };
                return u;
            }
        }

        private class CommentElement : NameTimeStringElement
        {
            protected override void OnCreateCell(UITableViewCell cell)
            {
                //Don't call the base since it will assign a background.
                return;
            }
        }
    }
}

