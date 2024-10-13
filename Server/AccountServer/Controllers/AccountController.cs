using AccountServer.DB;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AccountServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        AppDbContext context;

        public AccountController(AppDbContext context)
        {
            this.context = context;
        }

        [HttpPost]
        [Route("create")]
        public CreateAccountPacketRes CreateAccount([FromBody] CreateAccountPacketReq req)
        {
            CreateAccountPacketRes res = new CreateAccountPacketRes();

            AccountDb account = context.Accounts
                                    .AsNoTracking()
                                    .Where(a => a.AccountName == req.AccountName)
                                    .FirstOrDefault();

            if (account == null)
            {
                context.Accounts.Add(new AccountDb
                {
                    AccountName = req.AccountName,
                    Password = req.Password
                });

                bool success = context.SaveChangesEx();
                res.Success = success;
            }
            else
            {
                res.Success = false;
            }

            return res;
        }

        [HttpPost]
        [Route("login")]
        public LoginAccountPacketRes LoginAccount([FromBody] LoginAccountPacketReq req)
        {
            LoginAccountPacketRes res = new LoginAccountPacketRes();

            AccountDb account = context.Accounts
                                    .AsNoTracking()
                                    .Where(a => a.AccountName == req.AccountName && a.Password == req.Password)
                                    .FirstOrDefault();

            if (account == null)
            {
                res.Success = false;
            }
            else
            {
                res.Success = true;

                res.ServerList = new List<ServerInfo>
                {
                    new ServerInfo() { Name = "데포르쥬", Ip = "127.0.0.0", CrowdedLevel = 0 },
                    new ServerInfo() { Name = "아툰", Ip = "127.0.0.1", CrowdedLevel = 3 }
                };
            }

            return res;
        }
    }
}
