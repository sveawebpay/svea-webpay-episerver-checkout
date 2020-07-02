using Atata;

namespace Foundation.SystemTests.PageObjectModels.ManagerSite.Base
{
    using _ = HomeManagerPage;

    public class HomeManagerPage : BaseManagerPage<_>
    {
        [FindById("LoginCtrl_UserName")]
        public TextInput<_> UserName { get; private set; }

        [FindById("LoginCtrl_Password")]
        public PasswordInput<_> Password { get; private set; }

        [FindById("LoginCtrl_LoginButton")]
        public Clickable<ManagerPage, _> Login { get; private set; }
    }
}
