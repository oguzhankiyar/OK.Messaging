﻿using Microsoft.IdentityModel.Tokens;
using OK.Messaging.Common.Enumerations;
using OK.Messaging.Common.Models;
using OK.Messaging.Core.Managers;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;

namespace OK.Messaging.Engine.Managers
{
    public class AuthManager : IAuthManager
    {
        private readonly IUserManager _userManager;

        public AuthManager(IUserManager userManager)
        {
            _userManager = userManager;
        }

        public bool Login(string username, string password, out int userId)
        {
            userId = 0;

            UserModel user = _userManager.LoginUser(username, password);

            if (user == null)
            {
                UserModel userByUsername = _userManager.GetUserByUsername(username);

                if (userByUsername != null)
                {
                    _userManager.AddActivity(userByUsername.Id, ActivityTypeEnum.InvalidLogin, "Inivalid login.");
                }

                return false;
            }

            _userManager.AddActivity(user.Id, ActivityTypeEnum.SuccessLogin, "Success login.");

            userId = user.Id;

            return true;
        }

        public string CreateToken(int userId, string key, int expiresInMs)
        {
            var claims = new[] {
                new Claim(JwtRegisteredClaimNames.Jti, userId.ToString())
            };

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var creds = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.Now.AddMilliseconds(expiresInMs),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public int? GetUserIdByPrincipal(ClaimsPrincipal principal)
        {
            string userIdString = null;

            if (principal.HasClaim(claim => claim.Type == JwtRegisteredClaimNames.Jti))
            {
                userIdString = principal.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Jti).Value;
            }

            int userId;

            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out userId))
            {
                return null;
            }

            return userId;
        }

        public UserModel VerifyPrincipal(ClaimsPrincipal principal)
        {
            int? userId = GetUserIdByPrincipal(principal);

            if (!userId.HasValue)
            {
                return null;
            }

            return _userManager.GetUserById(userId.Value);
        }
    }
}