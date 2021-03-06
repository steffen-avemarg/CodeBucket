using CodeBucket.Bitbucket.Controllers;
using BitbucketSharp.Models;
using MonoTouch.UIKit;
using MonoTouch.Dialog;
using System.Linq;
using CodeBucket.Controllers;
using CodeFramework.Controllers;
using CodeFramework.Elements;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace CodeBucket.Bitbucket.Controllers.Groups
{
    public class GroupMembersController : BaseListModelController
    {
        public new List<UserModel> Model
        {
            get { return (List<UserModel>)base.Model; }
            set { base.Model = value; }
        }

        public string User { get; private set; }

        public string GroupName { get; private set; }
        
        public GroupMembersController(string user, string groupName)
            : base(typeof(List<UserModel>))
        {
            Style = UITableViewStyle.Plain;
            User = user;
            EnableSearch = true;
            SearchPlaceholder = "Search Memebers";
            NoItemsText = "No Members";
            Title = groupName;
            GroupName = groupName;
        }

        protected override object OnUpdateListModel(bool forced, int currentPage, ref int nextPage)
        {
            return Application.Client.Users[User].Groups[GroupName].GetInfo(forced).Members;
        }

        protected override Element CreateElement(object obj)
        {
            var s = (UserModel)obj;
            StyledElement sse = new UserElement(s.Username, s.FirstName, s.LastName, s.Avatar);
            sse.Tapped += () => NavigationController.PushViewController(new ProfileController(s.Username), true);
            return sse;
        }
    }
}

