using GamingWithMe.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamingWithMe.Application.Dtos
{

    public record RegisterDto(
        string email,
        string password,
        string username,
        UserType PlayerType
        );
    
}
