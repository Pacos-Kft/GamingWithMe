using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamingWithMe.Application.Dtos
{
    public enum PlayerType { Regular, Esport }

    public record RegisterDto(
        string email,
        string password,
        string username,
        PlayerType PlayerType
        );
    
}
