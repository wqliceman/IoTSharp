using IoTSharp.Contracts;
using IoTSharp.Data;

namespace IoTSharp.Dtos
{
    public class UserInfoDto
    {
        public ApiCode Code { get; set; }
        public string Roles { get; set; }
        public string Name { get; set; }

        public string Avatar { get; set; }
        public string Introduction { get; set; }
        public Customer Customer { get; set; }
        public Tenant Tenant { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
    }
}