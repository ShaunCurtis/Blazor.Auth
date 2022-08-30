I think you are overcomplicating this.  Here's a very basic login system I use for testing that demostrates how to incorporate the login component into App.  There's no need to capture which page the user is trying to access.  The user is already on it.

There's quitr a bit of code here as it's a working demo.  You

First classes to provide some test identities:
```
public static class TestIdentityProvider
{
    public const string Provider = "Dumb Provider";

    public static ClaimsIdentity GetIdentity(string userName)
    {
        var identity = identities.FirstOrDefault(item => item.Name.Equals(userName, StringComparison.OrdinalIgnoreCase));
        if (identity == null)
            return new ClaimsIdentity();

        return new ClaimsIdentity(identity.Claims, Provider);
    }

    private static List<TestIdentity> identities = new List<TestIdentity>()
        {
            VisitorIdentity, 
            UserIdentity, 
            AdminIdentity, 
        };

    public static List<string> GetTestIdentities()
    {
        var list = new List<string> { "None" };
        list.AddRange(identities.Select(identity => identity.Name!).ToList());
        return list;
    }

    public static Dictionary<Guid, string> TestIdentitiesDictionary()
    {
        var list = new Dictionary<Guid, string>();
        identities.ForEach(identity => list.Add(identity.Id, identity.Name));
        return list;
    }

    public static TestIdentity UserIdentity
        => new TestIdentity
        {
            Id = new Guid("10000000-0000-0000-0000-100000000001"),
            Name = "User-1",
            Role = "UserRole"
        };

    public static TestIdentity VisitorIdentity
        => new TestIdentity
        {
            Id = new Guid("10000000-0000-0000-0000-200000000001"),
            Name = "Visitor-1",
            Role = "VisitorRole"
        };

    public static TestIdentity AdminIdentity
        => new TestIdentity
        {
            Id = new Guid("10000000-0000-0000-0000-300000000001"),
            Name = "Admin-1",
            Role = "AdminRole"
        };
}
```
```csharp
public record TestIdentity
{
    public string Name { get; set; } = string.Empty;

    public Guid Id { get; set; } = Guid.Empty;

    public string Role { get; set; } = string.Empty;

    public Claim[] Claims
        => new[]{
            new Claim(ClaimTypes.Sid, this.Id.ToString()),
            new Claim(ClaimTypes.Name, this.Name),
            new Claim(ClaimTypes.Role, this.Role)
    };

}
```

And a very basic `AuthenticationStateProvider`:

```csharp
public class SimpleAuthenticationStateProvider : AuthenticationStateProvider
{
    ClaimsPrincipal? _user;

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
        => Task.FromResult(new AuthenticationState(_user ?? new ClaimsPrincipal()));

    public Task<AuthenticationState> ChangeIdentityAsync(string username)
    {
        _user = new ClaimsPrincipal(TestIdentityProvider.GetIdentity(username));
        var task = this.GetAuthenticationStateAsync();
        this.NotifyAuthenticationStateChanged(task);
        return task;
    }
}
```

The `Login` component:

```csharp
@implements IDisposable

@using System.Security.Claims;

<span class="me-2">Change User:</span>
<div class="w-25">
    <select id="userselect" class="form-control" @onchange="ChangeUser">
        @foreach (var value in TestIdentityProvider.GetTestIdentities())
        {
            @if (value == _currentUserName)
            {
                <option value="@value" selected>@value</option>
            }
            else
            {
                <option value="@value">@value</option>
            }
        }
    </select>
</div>

@code {
    [CascadingParameter] private Task<AuthenticationState> AuthTask { get; set; } = default!;

    [Inject] private AuthenticationStateProvider authState { get; set; } = default!;
    private SimpleAuthenticationStateProvider AuthState => (SimpleAuthenticationStateProvider)authState!;

    private ClaimsPrincipal user = new ClaimsPrincipal();
    private string _currentUserName = "None";

    protected async override Task OnInitializedAsync()
    {
        if (AuthTask is null)
            throw new ArgumentNullException("There's no cascaded Task<AuthenticationState> provided!");

        var authState = await AuthTask;
        this.user = authState.User;
        AuthState.AuthenticationStateChanged += this.OnUserChanged;
    }

    private async Task ChangeUser(ChangeEventArgs e)
        => await AuthState.ChangeIdentityAsync(e.Value?.ToString() ?? string.Empty);

    private async void OnUserChanged(Task<AuthenticationState> state)
        => await this.GetUser(state);

    private async Task GetUser(Task<AuthenticationState> state)
    {
        var authState = await state;
        this.user = authState.User;
    }

    public void Dispose()
        => AuthState.AuthenticationStateChanged -= this.OnUserChanged;
}
```

And modified `LoginDisplay`:

```csharp
<AuthorizeView>
    <span class="me-2">Hello, @(context.User.Identity?.Name!)</span><button class="btn btn-primary" @onclick=this.LogOut>Log Out</button>
</AuthorizeView>

@code {
    [Inject] private AuthenticationStateProvider? authState { get; set; }

    private SimpleAuthenticationStateProvider AuthState => (SimpleAuthenticationStateProvider)authState!;

    private async Task LogOut(MouseEventArgs e)
        => await AuthState.ChangeIdentityAsync(string.Empty);
}
```

The registered services:

```csharp
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddScoped<AuthenticationStateProvider, SimpleAuthenticationStateProvider>();
builder.Services.AddAuthorization();
builder.Services.AddSingleton<WeatherForecastService>();
```

And `App`:

```csharp
<CascadingAuthenticationState>
    <Router AppAssembly="@typeof(App).Assembly">
        <Found Context="routeData">
            <AuthorizeRouteView RouteData="@routeData" DefaultLayout="@typeof(MainLayout)">
                <Authorizing><h3>Trying to authorize you.</h3></Authorizing>
                <NotAuthorized>
                    <h3>Sorry mate, you can't go here now!</h3>
                    <Login />
                </NotAuthorized>
            </AuthorizeRouteView>
            <FocusOnNavigate RouteData="@routeData" Selector="h1" />
        </Found>
        <NotFound>
            <PageTitle>Not found</PageTitle>
            <LayoutView Layout="@typeof(MainLayout)">
                <p role="alert">Sorry, there's nothing at this address.</p>
            </LayoutView>
        </NotFound>
    </Router>
</CascadingAuthenticationState>
```



