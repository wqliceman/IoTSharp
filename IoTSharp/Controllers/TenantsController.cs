﻿using IoTSharp.Contracts;
using IoTSharp.Controllers.Models;
using IoTSharp.Data;
using IoTSharp.Extensions;
using IoTSharp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;

namespace IoTSharp.Controllers
{
    /// <summary>
    /// 租户管理
    /// </summary>
    [Route("api/[controller]")]
    [Authorize]
    [ApiController]
    public class TenantsController : ControllerBase
    {
        private ApplicationDbContext _context;
        private ILogger _logger;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;

        public TenantsController(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            ILogger<TenantsController> logger, ApplicationDbContext context
            )
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _context = context;
        }

        /// <summary>
        /// 产品列表
        /// </summary>
        /// <param name="m"></param>
        /// <returns></returns>
        [Authorize(Roles = nameof(UserRole.SystemAdmin))]
        [HttpGet]
        public async Task<ApiResult<PagedData<Tenant>>> GetTenant([FromQuery] QueryDto m)
        {
            var profile = this.GetUserProfile();
            var querym = _context.Tenant.Where(c => c.Deleted == false);
            var data = await m.Query(querym, c => c.Name);
            return new ApiResult<PagedData<Tenant>>(ApiCode.Success, "OK", data);
        }

        /// <summary>
        /// 普通用户用于活的自身的租户信息
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [Authorize(Roles = nameof(UserRole.NormalUser))]
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResult), StatusCodes.Status404NotFound)]
        [ProducesDefaultResponseType]
        public async Task<ApiResult<Tenant>> GetTenant(Guid id)
        {
            var tenant = await _context.Tenant.FindAsync(id);
            if (tenant == null)
            {
                return new ApiResult<Tenant>(ApiCode.CantFindObject, "can't find this object", null);
            }

            return new ApiResult<Tenant>(ApiCode.Success, "OK", tenant);
        }

        /// <summary>
        /// 修改指定的租户信息， 仅限租户管理员
        /// </summary>
        /// <param name="id"></param>
        /// <param name="tenant"></param>
        /// <returns></returns>
        [Authorize(Roles = nameof(UserRole.TenantAdmin))]
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResult<Tenant>), StatusCodes.Status404NotFound)]
        [ProducesDefaultResponseType]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ApiResult<Tenant>> PutTenant(Guid id, Tenant tenant)
        {
            if (id != tenant.Id)
            {
                return new ApiResult<Tenant>(ApiCode.InValidData, "InValidData", tenant);
            }

            _context.Entry(tenant).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                if (!_context.Tenant.Any(c => c.Id == id && c.Deleted == false))
                {
                    return new ApiResult<Tenant>(ApiCode.CantFindObject, "cant't find this object", tenant);
                }
                else
                {
                    return new ApiResult<Tenant>(ApiCode.Exception, ex.Message, tenant);
                }
            }
            catch (Exception ex)
            {
                return new ApiResult<Tenant>(ApiCode.Exception, ex.Message, tenant);
            }

            return new ApiResult<Tenant>(ApiCode.Success, "Ok", tenant);
        }

        /// <summary>
        /// 新增租户， 仅限系统管理员
        /// </summary>
        /// <param name="tenant"></param>
        /// <returns></returns>
        [Authorize(Roles = nameof(UserRole.SystemAdmin))]
        // POST: api/Tenants
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResult<Tenant>), StatusCodes.Status400BadRequest)]
        [ProducesDefaultResponseType]
        public async Task<ApiResult<Tenant>> PostTenant(Tenant tenant)
        {
            try
            {
                _context.Tenant.Add(tenant);
                await _context.SaveChangesAsync();
                return new ApiResult<Tenant>(ApiCode.Success, "Ok", tenant);
            }
            catch (Exception ex)
            {
                return new ApiResult<Tenant>(ApiCode.Exception, ex.Message, tenant);
            }
        }

        /// <summary>
        /// 删除租户，仅限系统用户
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [Authorize(Roles = nameof(UserRole.SystemAdmin))]
        // DELETE: api/Tenants/5
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResult<Tenant>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResult<Tenant>), StatusCodes.Status404NotFound)]
        [ProducesDefaultResponseType]
        public async Task<ApiResult<Tenant>> DeleteTenant(Guid id)
        {
            var tenant = await _context.Tenant.FindAsync(id);
            if (tenant == null)
            {
                return new ApiResult<Tenant>(ApiCode.CantFindObject, "Not found tenant", tenant);
            }
            try
            {
                tenant.Deleted = true;
                _context.Tenant.Update(tenant);
                await _context.SaveChangesAsync();
                return new ApiResult<Tenant>(ApiCode.Success, "Ok", tenant);
            }
            catch (Exception ex)
            {
                return new ApiResult<Tenant>(ApiCode.Exception, ex.Message, tenant);
            }
        }
    }
}