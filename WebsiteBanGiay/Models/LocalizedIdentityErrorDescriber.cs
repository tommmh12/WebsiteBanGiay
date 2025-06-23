using Microsoft.AspNetCore.Identity;

namespace WebsiteBanGiay.Models
{
    public class LocalizedIdentityErrorDescriber : IdentityErrorDescriber
    {
        public override IdentityError DefaultError() => new() { Code = nameof(DefaultError), Description = "Đã xảy ra lỗi không xác định." };
        public override IdentityError PasswordRequiresDigit() => new() { Code = nameof(PasswordRequiresDigit), Description = "Mật khẩu phải chứa ít nhất một chữ số (0-9)." };
    }
}
