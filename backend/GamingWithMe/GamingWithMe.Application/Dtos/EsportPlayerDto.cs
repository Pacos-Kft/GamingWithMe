using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamingWithMe.Application.Dtos
{
    public record EsportPlayerDto(
        string username, 
        string avatarurl, 
        string bio, 
        List<string> games,
        List<string> languages, 
        int earnings, 
        DateTime joined);
}
