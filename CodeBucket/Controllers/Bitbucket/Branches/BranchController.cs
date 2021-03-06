using MonoTouch.Dialog;
using BitbucketSharp.Models;
using System.Collections.Generic;
using MonoTouch.UIKit;
using CodeBucket.Bitbucket.Controllers;
using CodeBucket.Bitbucket.Controllers.Source;
using System.Linq;
using CodeBucket.Controllers;
using CodeFramework.Controllers;
using CodeFramework.Elements;
using System.Threading.Tasks;

namespace CodeBucket.Bitbucket.Controllers.Branches
{
    public class BranchController : BaseListModelController
	{
        public string Username { get; private set; }

        public string Slug { get; private set; }

		public BranchController(string username, string slug) 
            : base(typeof(List<BranchModel>))
		{
            Username = username;
            Slug = slug;
            Title = "Branches";
            SearchPlaceholder = "Search Branches";
            NoItemsText = "No Branches";
            Style = UITableViewStyle.Plain;
		}

        protected override Element CreateElement(object obj)
        {
            var branchModel = (BranchModel)obj;
            return new StyledElement(branchModel.Branch, () => NavigationController.PushViewController(new SourceController(Username, Slug, branchModel.Branch), true));
        }

        protected override object OnUpdateListModel(bool forced, int currentPage, ref int nextPage)
        {
            nextPage = -1;
            return Application.Client.Users[Username].Repositories[Slug].Branches.GetBranches(forced).Values.OrderBy(x => x.Branch).ToList();
        }
	}
}

