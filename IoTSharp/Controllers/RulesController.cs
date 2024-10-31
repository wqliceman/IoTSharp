﻿using IoTSharp.Contracts;
using IoTSharp.Controllers.Models;
using IoTSharp.Data;
using IoTSharp.Dtos;
using IoTSharp.Extensions;
using IoTSharp.FlowRuleEngine;
using IoTSharp.Models;
using IoTSharp.Models.Rule;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ShardingCore.Extensions;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace IoTSharp.Controllers
{
    [Route("api/[controller]/[action]")]
    [Authorize]
    [ApiController]
    public class RulesController : ControllerBase
    {
        private ApplicationDbContext _context;
        private readonly FlowRuleProcessor _flowRuleProcessor;
        private readonly TaskExecutorHelper _helper;
        private UserManager<IdentityUser> _userManager;

        public RulesController(ApplicationDbContext context, UserManager<IdentityUser> userManager, FlowRuleProcessor flowRuleProcessor, TaskExecutorHelper helper)
        {
            this._userManager = userManager;
            this._context = context;
            _flowRuleProcessor = flowRuleProcessor;
            _helper = helper;
        }

        /// <summary>
        /// 更新节点的条件表达式
        /// </summary>
        /// <returns> </returns>
        ///

        [HttpPost]
        public async Task<ApiResult<bool>> UpdateFlowExpression(UpdateFlowExpression m)
        {
            var profile = this.GetUserProfile();
            var flow = await _context.Flows.SingleOrDefaultAsync(c => c.FlowId == m.FlowId && c.Tenant.Id == profile.Tenant);
            if (flow != null)
            {
                flow.Conditionexpression = m.Expression;
                _context.Flows.Update(flow);
                await _context.SaveChangesAsync();

                return new ApiResult<bool>(ApiCode.Success, "Ok", true);
            }
            return new ApiResult<bool>(ApiCode.InValidData, "can't find this object", false);
        }

        [HttpPost]
        public ApiResult<PagedData<FlowRule>> Index([FromBody] RulePageParam m)
        {
            var profile = this.GetUserProfile();

            Expression<Func<FlowRule, bool>> condition = x => x.RuleStatus > -1 && x.Tenant.Id == profile.Tenant;
            if (!string.IsNullOrEmpty(m.Name))
            {
                condition = condition.And(x => x.Name.Contains(m.Name));
            }

            if (m.CreatTime != null && m.CreatTime.Length == 2)
            {
                condition = condition.And(x => x.CreatTime > m.CreatTime[0] && x.CreatTime < m.CreatTime[1]);
            }

            if (!string.IsNullOrEmpty(m.Creator))
            {
                condition = condition.And(x => x.Creator == m.Creator);
            }

            return new ApiResult<PagedData<FlowRule>>(ApiCode.Success, "OK", new PagedData<FlowRule>
            {
                total = _context.FlowRules.Count(condition),
                rows = _context.FlowRules.OrderByDescending(c => c.CreatTime).Where(condition).Skip((m.Offset) * m.Limit).Take(m.Limit).ToList()
            });
        }

        [HttpPost]
        public async Task<ApiResult<bool>> Save(FlowRule m)
        {
            var profile = this.GetUserProfile();
            try
            {
                m.MountType = m.MountType;
                m.RuleStatus = 1;
                _context.JustFill(this, m);
                m.CreatTime = DateTime.UtcNow;
                _context.FlowRules.Add(m);
                await _context.SaveChangesAsync();
                return new ApiResult<bool>(ApiCode.Success, "OK", true);
            }
            catch (Exception ex)
            {
                return new ApiResult<bool>(ApiCode.Exception, ex.Message, false);
            }
        }

        [HttpPost]
        public async Task<ApiResult<bool>> Update(FlowRule m)
        {
            var profile = this.GetUserProfile();
            var flowrule = _context.FlowRules.SingleOrDefault(c => c.RuleId == m.RuleId && c.Tenant.Id == profile.Tenant);
            if (flowrule != null)
            {
                try
                {
                    flowrule.MountType = m.MountType;
                    flowrule.Name = m.Name;
                    flowrule.RuleDesc = m.RuleDesc;
                    _context.FlowRules.Update(flowrule);
                    await _context.SaveChangesAsync();
                    return new ApiResult<bool>(ApiCode.Success, "OK", true);
                }
                catch (Exception ex)
                {
                    return new ApiResult<bool>(ApiCode.Exception, ex.Message, false);
                }
            }
            return new ApiResult<bool>(ApiCode.Success, "can't find this object", false);
        }

        [HttpGet]
        public async Task<ApiResult<bool>> Delete(Guid id)
        {
            var profile = this.GetUserProfile();
            var rule = _context.FlowRules.SingleOrDefault(c => c.RuleId == id && c.Tenant.Id == profile.Tenant);
            if (rule != null)
            {
                try
                {
                    _context.FlowOperations.RemoveRange(_context.FlowOperations.Where(c => c.FlowRule.RuleId == id).ToList());
                    await _context.SaveChangesAsync();
                    _context.BaseEvents.RemoveRange(_context.BaseEvents.Where(c => c.FlowRule.RuleId == id).ToList());
                    await _context.SaveChangesAsync();
                    _context.Flows.RemoveRange(_context.Flows.Where(c => c.FlowRule.RuleId == id).ToList());
                    await _context.SaveChangesAsync();
                    _context.DeviceRules.RemoveRange(_context.DeviceRules.Where(c => c.FlowRule.RuleId == id).ToList());
                    await _context.SaveChangesAsync();
                    _context.FlowRules.Remove(rule);
                    await _context.SaveChangesAsync();
                    return new ApiResult<bool>(ApiCode.Success, "OK", true);
                }
                catch (Exception ex)
                {
                    return new ApiResult<bool>(ApiCode.Exception, ex.Message, false);
                }
            }

            return new ApiResult<bool>(ApiCode.Success, "can't find this object", false);
        }

        [HttpGet]
        public ApiResult<FlowRule> Get(Guid id)
        {
            var profile = this.GetUserProfile();
            var rule = _context.FlowRules.SingleOrDefault(c => c.RuleId == id && c.Tenant.Id == profile.Tenant);
            if (rule != null)
            {
                return new ApiResult<FlowRule>(ApiCode.Success, "OK", rule);
            }

            return new ApiResult<FlowRule>(ApiCode.CantFindObject, "can't find this object", null);
        }

        /// <summary>
        /// 复制一个规则副本
        /// </summary>
        /// <param name="flowRule"></param>
        /// <returns></returns>

        [HttpPost]
        public async Task<ApiResult<bool>> Fork(FlowRule flowRule)
        {
            var profile = this.GetUserProfile();
            var rule = await _context.FlowRules.SingleOrDefaultAsync(c => c.RuleId == flowRule.RuleId && c.Tenant.Id == profile.Tenant);
            if (rule != null)
            {
                var _customer = await _context.Customer.SingleOrDefaultAsync(c => c.Id == profile.Customer);
                var _tenant = await _context.Tenant.SingleOrDefaultAsync(c => c.Id == profile.Tenant);
                var newrule = new FlowRule();
                newrule.DefinitionsXml = rule.DefinitionsXml;
                newrule.Describes = flowRule.Describes;
                //     newrule.Creator = profile.Id.ToString();
                newrule.Name = flowRule.Name;
                newrule.CreatTime = DateTime.UtcNow;
                newrule.ExecutableCode = rule.ExecutableCode;
                newrule.RuleDesc = flowRule.RuleDesc;
                newrule.RuleStatus = 1;
                newrule.MountType = rule.MountType;
                newrule.ParentRuleId = rule.RuleId;
                newrule.CreateId = new Guid();
                newrule.SubVersion = rule.SubVersion + 0.01;

                newrule.Customer = _customer;
                newrule.Tenant = _tenant;
                newrule.Creator = profile.Id.ToString();
                _context.FlowRules.Add(newrule);
                await _context.SaveChangesAsync();

                var flows = _context.Flows.Where(c => c.FlowRule.RuleId == rule.RuleId && c.CreateId == rule.CreateId).ToList();
                var newflows = flows.Select(c => new Flow()
                {
                    FlowRule = newrule,
                    Conditionexpression = c.Conditionexpression,
                    CreateDate = DateTime.UtcNow,
                    FlowStatus = 1,
                    FlowType = c.FlowType,
                    Flowdesc = c.Flowdesc,
                    Incoming = c.Incoming,
                    Flowname = c.Flowname,
                    NodeProcessClass = c.NodeProcessClass,
                    NodeProcessMethod = c.NodeProcessMethod,
                    NodeProcessParams = c.NodeProcessParams,
                    NodeProcessScript = c.NodeProcessScript,
                    NodeProcessScriptType = c.NodeProcessScriptType,
                    NodeProcessType = c.NodeProcessType,
                    ObjectId = c.ObjectId,
                    Outgoing = c.Outgoing,
                    SourceId = c.SourceId,
                    TargetId = c.TargetId,
                    Customer = _customer,
                    Tenant = _tenant,
                    bpmnid = c.bpmnid,
                    CreateId = newrule.CreateId
                }).ToList();
                if (newflows.Count > 0)
                {
                    _context.Flows.AddRange(newflows);
                    await _context.SaveChangesAsync();
                }

                return new ApiResult<bool>(ApiCode.Success, "Ok", true);
            }
            else
            {
            }

            return new ApiResult<bool>(ApiCode.CantFindObject, "can't find this object", false);
        }

        [HttpPost]
        public ApiResult<bool> BindDevice(ModelRuleBind m)
        {
            var profile = this.GetUserProfile();
            if (m.dev != null)

            {
                foreach (var d in m.dev.ToList())
                {
                    if (!_context.DeviceRules.Any(c => c.FlowRule.RuleId == m.rule && c.Device.Id == d))
                    {
                        var dev = _context.Device.SingleOrDefault(c => c.Id == d && c.Tenant.Id == profile.Tenant);
                        var rule = _context.FlowRules.SingleOrDefault(c =>
                            c.RuleId == m.rule && c.Tenant.Id == profile.Tenant);
                        if (dev != null)
                        {
                            if (rule != null)
                            {
                                var dr = new DeviceRule();
                                dr.Device = dev;
                                dr.FlowRule = rule;
                                dr.ConfigDateTime = DateTime.UtcNow;
                                dr.ConfigUser = profile.Id;
                                _context.DeviceRules.Add(dr);
                            }
                            else
                            {
                                return new ApiResult<bool>(ApiCode.CantFindObject, "can not found that rule:" + m.rule, false);
                            }
                        }
                        else
                        {
                            return new ApiResult<bool>(ApiCode.CantFindObject, "can not found that device:" + d, false);
                        }
                    }
                }
                _context.SaveChanges();
                return new ApiResult<bool>(ApiCode.Success, "rule binding success", true);
            }

            return new ApiResult<bool>(ApiCode.CantFindObject, "No device found", false);
        }

        [HttpGet]
        public async Task<ApiResult<bool>> DeleteDeviceRules(Guid deviceId, Guid ruleId)
        {
            var profile = this.GetUserProfile();
            var map = await _context.DeviceRules.Include(c => c.Device)
                .Include(c => c.FlowRule).FirstOrDefaultAsync(c => c.FlowRule.RuleId == ruleId && c.Device.Id == deviceId && c.Device.Tenant.Id == profile.Tenant && c.FlowRule.Tenant.Id == profile.Tenant);
            if (map != null)
            {
                _context.DeviceRules.Remove(map);
                _context.SaveChanges();
                return new ApiResult<bool>(ApiCode.Success, "rule has been removed", true);
            }
            return new ApiResult<bool>(ApiCode.CantFindObject, "this mapping was not found", true);
        }

        [HttpGet]
        public ApiResult<List<FlowRule>> GetDeviceRules(Guid deviceId)
        {
            var profile = this.GetUserProfile();
            return new ApiResult<List<FlowRule>>(ApiCode.Success, "Ok", _context.DeviceRules.Include(c => c.Device).Where(c => c.Device.Id == deviceId && c.Device.Tenant.Id == profile.Tenant).Select(c => c.FlowRule).Select(c => new FlowRule() { RuleId = c.RuleId, CreatTime = c.CreatTime, Name = c.Name, RuleDesc = c.RuleDesc }).ToList());
        }

        [HttpGet]
        public async Task<ApiResult<PagedData<DeviceRuleDto>>> GetRuleDevices([FromQuery] DeviceParam m)
        {
            var profile = this.GetUserProfile();
            Expression<Func<DeviceRule, bool>> condition = x => x.Device.Customer.Id == profile.Customer && !x.Device.Deleted && x.Device.Tenant.Id == profile.Tenant && x.FlowRule.RuleId == m.ruleId;
            if (!string.IsNullOrEmpty(m.Name))
            {
                if (System.Text.RegularExpressions.Regex.IsMatch(m.Name, @"(?im)^[{(]?[0-9A-F]{8}[-]?(?:[0-9A-F]{4}[-]?){3}[0-9A-F]{12}[)}]?$"))
                {
                    condition = condition.And(x => x.Device.Id == Guid.Parse(m.Name));
                }
                else
                {
                    condition = condition.And(x => x.Device.Name.Contains(m.Name));
                }
            }

            var rows = await _context.DeviceRules.Include(c => c.FlowRule).Include(c => c.Device).Where(condition)
                .Select(c => new DeviceRuleDto()
                {
                    Id = c.Device.Id,
                    DeviceType = c.Device.DeviceType,
                    EnableTrace = c.EnableTrace,
                    Name = c.Device.Name,
                    Timeout = c.Device.Timeout
                }).Skip(m.Offset * m.Limit).Take(m.Limit).ToListAsync();
            var total = await _context.DeviceRules.Include(c => c.FlowRule).Include(c => c.Device).Where(condition).Select(c => c.Device).CountAsync();
            return new ApiResult<PagedData<DeviceRuleDto>>(ApiCode.Success, "Ok", new PagedData<DeviceRuleDto> { rows = rows, total = total });
        }

        [HttpGet]
        public ApiResult<List<Flow>> GetFlows(Guid ruleId)
        {
            var tid = User.GetTenantId();
            return new ApiResult<List<Flow>>(ApiCode.Success, "Ok",
                _context.Flows.Include(c => c.FlowRule)
                .Where(c => c.FlowRule.RuleId == ruleId && c.FlowStatus > 0 && c.Tenant.Id == tid).ToList());
        }

        [HttpPost]
        public ApiResult<bool> SaveDiagram(ModelWorkFlow m)
        {
            var profile = this.GetUserProfile();
            var activity = JsonConvert.DeserializeObject<Activity>(m.Biz);
            var CreatorId = Guid.NewGuid();
            var CreateDate = DateTime.UtcNow;
            var rule = _context.FlowRules.Include(c => c.Customer).Include(c => c.Tenant).FirstOrDefault(c => c.RuleId == activity.RuleId);
            rule.DefinitionsXml = m.Xml;
            rule.Creator = profile.Id.ToString();
            rule.CreateId = CreatorId;
            _context.Flows.Where(c => c.FlowRule.RuleId == rule.RuleId).ToList().ForEach(c =>
            {
                c.FlowStatus = -1;
            });
            _context.FlowRules.Update(rule);
            _context.SaveChanges();
            if (activity.BaseBpmnObjects != null)
            {
                var fw = activity.BaseBpmnObjects.Select(c => new Flow
                {
                    FlowRule = rule,
                    Flowname = c.BizObject.Flowname,
                    bpmnid = c.id,
                    FlowType = c.bpmntype,
                    NodeProcessScript = c.BizObject.flowscript,
                    NodeProcessScriptType = c.BizObject.flowscripttype,
                    FlowStatus = 1,
                    CreateId = CreatorId,
                    Createor = profile.Id,
                    CreateDate = CreateDate,
                    Customer = rule.Customer,
                    Tenant = rule.Tenant
                });

                _context.Flows.AddRange(fw);
                _context.SaveChanges();
            }

            if (activity.StartEvents != null)
            {
                var fw = activity.StartEvents.Select(c => new Flow
                {
                    FlowRule = rule,
                    Flowname = c.BizObject.Flowname,
                    bpmnid = c.id,
                    FlowType = c.bpmntype,
                    FlowStatus = 1,
                    CreateId = CreatorId,
                    Createor = profile.Id,
                    CreateDate = CreateDate,
                    Customer = rule.Customer,
                    Tenant = rule.Tenant
                });

                _context.Flows.AddRange(fw);
                _context.SaveChanges();
            }

            if (activity.EndEvents != null)
            {
                var fw = activity.EndEvents.Select(c => new Flow
                {
                    FlowRule = rule,
                    Flowname = c.BizObject.Flowname,
                    bpmnid = c.id,
                    FlowType = c.bpmntype,
                    FlowStatus = 1,
                    CreateId = CreatorId,
                    Createor = profile.Id,
                    CreateDate = CreateDate,
                    Customer = rule.Customer,
                    Tenant = rule.Tenant
                });

                _context.Flows.AddRange(fw);
                _context.SaveChanges();
            }

            if (activity.SequenceFlows != null)
            {
                var fw = activity.SequenceFlows.Select(c => new Flow
                {
                    FlowRule = rule,
                    Flowname = c.BizObject.Flowname,
                    bpmnid = c.id,
                    FlowType = c.bpmntype,
                    SourceId = c.sourceId,
                    TargetId = c.targetId,
                    FlowStatus = 1,
                    CreateId = CreatorId,
                    Conditionexpression = c.BizObject.conditionexpression,
                    NodeProcessParams = c.BizObject.NodeProcessParams,
                    Createor = profile.Id,
                    CreateDate = CreateDate,
                    Customer = rule.Customer,
                    Tenant = rule.Tenant
                });

                _context.Flows.AddRange(fw);
                _context.SaveChanges();
            }

            if (activity.Tasks != null)
            {
                var fw = activity.Tasks.Select(c => new Flow
                {
                    FlowRule = rule,
                    Flowname = c.BizObject.Flowname,
                    bpmnid = c.id,
                    FlowType = c.bpmntype,
                    NodeProcessParams = c.BizObject.NodeProcessParams,
                    NodeProcessClass = c.BizObject.NodeProcessClass,
                    NodeProcessScript = c.BizObject.flowscript,
                    NodeProcessScriptType = c.BizObject.flowscripttype,
                    FlowStatus = 1,
                    CreateId = CreatorId,
                    Createor = profile.Id,
                    CreateDate = CreateDate,
                    Customer = rule.Customer,
                    Tenant = rule.Tenant,
                    Flowdesc = JsonConvert.SerializeObject(c.BizObject.profile ?? new object())
                });

                _context.Flows.AddRange(fw);
                _context.SaveChanges();
            }

            if (activity.DataInputAssociations != null)
            {
                var fw = activity.DataInputAssociations.Select(c => new Flow
                {
                    FlowRule = rule,
                    Flowname = c.BizObject.Flowname,
                    bpmnid = c.id,
                    FlowType = c.bpmntype,
                    FlowStatus = 1,
                    CreateId = CreatorId,
                    Createor = profile.Id,
                    CreateDate = CreateDate,
                    Customer = rule.Customer,
                    Tenant = rule.Tenant
                });

                _context.Flows.AddRange(fw);
                _context.SaveChanges();
            }

            if (activity.DataOutputAssociations != null)
            {
                var fw = activity.DataOutputAssociations.Select(c => new Flow
                {
                    FlowRule = rule,
                    Flowname = c.BizObject.Flowname,
                    bpmnid = c.id,
                    FlowType = c.bpmntype,
                    FlowStatus = 1,
                    CreateId = CreatorId,
                    Createor = profile.Id,
                    CreateDate = CreateDate,
                    Customer = rule.Customer,
                    Tenant = rule.Tenant
                });

                _context.Flows.AddRange(fw);
                _context.SaveChanges();
            }

            if (activity.TextAnnotations != null)
            {
                var fw = activity.TextAnnotations.Select(c => new Flow
                {
                    FlowRule = rule,
                    Flowname = c.BizObject.Flowname,
                    bpmnid = c.id,
                    FlowType = c.bpmntype,
                    FlowStatus = 1,
                    CreateId = CreatorId,
                    Createor = profile.Id,
                    CreateDate = CreateDate,
                    Customer = rule.Customer,
                    Tenant = rule.Tenant
                });

                _context.Flows.AddRange(fw);
                _context.SaveChanges();
            }

            if (activity.Containers != null)
            {
                var fw = activity.Containers.Select(c => new Flow
                {
                    FlowRule = rule,
                    Flowname = c.BizObject.Flowname,
                    bpmnid = c.id,
                    FlowType = c.bpmntype,
                    FlowStatus = 1,
                    CreateId = CreatorId,
                    Createor = profile.Id,
                    CreateDate = CreateDate,
                    Customer = rule.Customer,
                    Tenant = rule.Tenant
                });

                _context.Flows.AddRange(fw);
                _context.SaveChanges();
            }

            if (activity.GateWays != null)
            {
                _context.Flows.AddRange(activity.GateWays.Select(c => new Flow
                {
                    FlowRule = rule,
                    Flowname = c.BizObject.Flowname,
                    bpmnid = c.id,
                    FlowType = c.bpmntype,
                    FlowStatus = 1,
                    CreateId = CreatorId,
                    Createor = profile.Id,
                    CreateDate = CreateDate,
                    Customer = rule.Customer,
                    Tenant = rule.Tenant
                }).ToList());
                _context.SaveChanges();
            }

            if (activity.DataStoreReferences != null)
            {
                var fw = activity.DataStoreReferences.Select(c => new Flow
                {
                    FlowRule = rule,
                    Flowname = c.BizObject.Flowname,
                    bpmnid = c.id,
                    FlowType = c.bpmntype,
                    FlowStatus = 1,
                    CreateId = CreatorId,
                    Createor = profile.Id,
                    CreateDate = CreateDate,
                    Customer = rule.Customer,
                    Tenant = rule.Tenant
                });

                _context.Flows.AddRange(fw);
                _context.SaveChanges();
            }

            if (activity.Lane != null)
            {
                var fw = activity.Lane.Select(c => new Flow
                {
                    FlowRule = rule,
                    Flowname = c.BizObject.Flowname,
                    bpmnid = c.id,
                    FlowType = c.bpmntype,
                    FlowStatus = 1,
                    CreateId = CreatorId,
                    Createor = profile.Id,
                    CreateDate = CreateDate,
                    Customer = rule.Customer,
                    Tenant = rule.Tenant
                });

                _context.Flows.AddRange(fw);
                _context.SaveChanges();
            }

            if (activity.LaneSet != null)
            {
                var fws = activity.LaneSet.Select(c => new Flow
                {
                    FlowRule = rule,
                    Flowname = c.BizObject.Flowname,
                    bpmnid = c.id,
                    FlowType = c.bpmntype,
                    FlowStatus = 1,
                    CreateId = CreatorId,
                    Createor = profile.Id,
                    CreateDate = CreateDate,
                    Customer = rule.Customer,
                    Tenant = rule.Tenant
                });

                _context.Flows.AddRange(fws);
                _context.SaveChanges();
            }
            return new ApiResult<bool>(ApiCode.Success, "Ok", true);
        }

        [HttpPost]
        public ApiResult<bool> SaveDiagramV(ModelDiagram m)
        {
            var profile = this.GetUserProfile();
            try
            {
                var CreatorId = Guid.NewGuid();
                var CreateDate = DateTime.UtcNow;
                var rule = _context.FlowRules.Include(c => c.Customer).Include(c => c.Tenant).FirstOrDefault(c => c.RuleId == m.RuleId);
                rule.Creator = profile.Id.ToString();
                rule.CreateId = CreatorId;
                _context.Flows.Where(c => c.FlowRule.RuleId == rule.RuleId).ToList().ForEach(c =>
                {
                    c.FlowStatus = -1;
                });
                _context.FlowRules.Update(rule);
                _context.SaveChanges();
                foreach (var item in m.nodes)
                {
                    switch (item.nodetype)
                    {
                        case "basic":
                            {
                                var node = new Flow
                                {
                                    FlowRule = rule,
                                    Flowname = item.name,
                                    bpmnid = item.nodeId,
                                    FlowType = item.nodenamespace,
                                    FlowStatus = 1,
                                    CreateId = CreatorId,
                                    Createor = profile.Id,
                                    CreateDate = CreateDate,
                                    Customer = rule.Customer,
                                    Tenant = rule.Tenant,

                                    FlowClass = item.nodeclass,
                                    FlowNameSpace = item.nodetype,
                                    FlowIcon = item.icon,
                                    Top = item.top,
                                    Left = item.left
                                };
                                _context.Flows.AddRange(node);
                                _context.SaveChanges();
                            }
                            break;

                        case "executor":
                            {
                                var node = new Flow
                                {
                                    FlowRule = rule,
                                    Flowname = item.name,
                                    bpmnid = item.nodeId,
                                    FlowType = item.nodenamespace,
                                    NodeProcessParams = item.content,
                                    NodeProcessScriptType = item.nodetype,

                                    NodeProcessClass = item.mata,
                                    FlowStatus = 1,
                                    CreateId = CreatorId,
                                    Createor = profile.Id,
                                    CreateDate = CreateDate,
                                    Customer = rule.Customer,
                                    Tenant = rule.Tenant,

                                    FlowClass = item.nodeclass,
                                    FlowNameSpace = item.nodetype,
                                    FlowIcon = item.icon,
                                    Top = item.top,
                                    Left = item.left
                                };
                                _context.Flows.AddRange(node);
                                _context.SaveChanges();
                            }
                            break;

                        case "script":
                            {
                                var node = new Flow
                                {
                                    FlowRule = rule,
                                    Flowname = item.name,
                                    bpmnid = item.nodeId,
                                    FlowType = item.nodenamespace,
                                    NodeProcessScript = item.content,
                                    NodeProcessScriptType = item.mata,
                                    FlowStatus = 1,
                                    CreateId = CreatorId,
                                    Createor = profile.Id,
                                    CreateDate = CreateDate,
                                    Customer = rule.Customer,
                                    Tenant = rule.Tenant,
                                    FlowClass = item.nodeclass,
                                    FlowNameSpace = item.nodetype,
                                    FlowIcon = item.icon,
                                    Top = item.top,
                                    Left = item.left
                                };
                                _context.Flows.AddRange(node);
                                _context.SaveChanges();
                            }
                            break;
                    }
                }

                foreach (var item in m.lines)
                {
                    var node = new Flow
                    {
                        FlowRule = rule,
                        Flowname = item.linename,
                        bpmnid = item.lineId,
                        FlowType = item.linenamespace,
                        TargetId = item.targetId,
                        SourceId = item.sourceId,
                        Conditionexpression = item.condition,
                        FlowNameSpace = "line",
                        FlowStatus = 1,
                        CreateId = CreatorId,
                        Createor = profile.Id,
                        CreateDate = CreateDate,
                        Customer = rule.Customer,
                        Tenant = rule.Tenant
                    };
                    _context.Flows.AddRange(node);
                    _context.SaveChanges();
                }

                return new ApiResult<bool>(ApiCode.Success, "Ok", true);
            }
            catch (Exception exception)
            {
                return new ApiResult<bool>(ApiCode.Exception, exception.Message, false);
            }
        }

        [HttpGet]
        public async Task<ApiResult<ModelDiagram>> GetDiagramV(Guid id)
        {
            var profile = this.GetUserProfile();

            try
            {
                var flows = await _context.Flows
                    .Where(c => c.FlowRule.RuleId == id && c.FlowStatus > 0 && c.Tenant.Id == profile.Tenant)
                    .ToListAsync();
                ModelDiagram m = new ModelDiagram();
                m.lines = new List<LineObject>();
                m.nodes = new List<NodeObject>();
                foreach (var item in flows)
                {
                    switch (item.FlowNameSpace)
                    {
                        case "line":
                            {
                                m.lines.Add(new LineObject()
                                {
                                    sourceId = item.SourceId,
                                    linename = item.Flowname,
                                    targetId = item.TargetId,
                                    condition = item.Conditionexpression,
                                    linenamespace = item.FlowType,
                                    lineId = item.bpmnid,
                                });
                            }
                            break;

                        case "basic":
                            {
                                m.nodes.Add(new NodeObject()
                                {
                                    nodeId = item.bpmnid,
                                    nodetype = item.FlowNameSpace,
                                    name = item.Flowname,
                                    nodeclass = item.FlowClass,
                                    nodenamespace = item.FlowType,
                                    icon = item.FlowIcon,
                                    top = item.Top,
                                    left = item.Left
                                });
                            }
                            break;

                        case "script":
                            {
                                m.nodes.Add(new NodeObject()
                                {
                                    nodeId = item.bpmnid,
                                    nodetype = item.FlowNameSpace,
                                    name = item.Flowname,
                                    content = item.NodeProcessScript,
                                    mata = item.NodeProcessScriptType,
                                    nodeclass = item.FlowClass,
                                    nodenamespace = item.FlowType,
                                    icon = item.FlowIcon,
                                    top = item.Top,
                                    left = item.Left
                                });
                            }
                            break;

                        case "executor":
                            {
                                m.nodes.Add(new NodeObject()
                                {
                                    nodeId = item.bpmnid,
                                    nodetype = item.FlowNameSpace,
                                    name = item.Flowname,
                                    content = item.NodeProcessParams,
                                    mata = item.NodeProcessClass,
                                    nodeclass = item.FlowClass,
                                    nodenamespace = item.FlowType,
                                    icon = item.FlowIcon,
                                    top = item.Top,
                                    left = item.Left
                                });
                            }
                            break;
                    }
                }

                return new ApiResult<ModelDiagram>(ApiCode.Success, "Ok", m);
            }
            catch (Exception exception)
            {
                return new ApiResult<ModelDiagram>(ApiCode.Exception, exception.Message, null);
            }
        }

        [HttpGet]
        public async Task<ApiResult<Activity>> GetDiagram(Guid id)
        {
            var profile = this.GetUserProfile();
            var ruleflow = await _context.FlowRules.FirstOrDefaultAsync(c => c.RuleId == id && c.Tenant.Id == profile.Tenant);
            Activity activity = new Activity();

            activity.SequenceFlows ??= new List<SequenceFlow>();
            activity.GateWays ??= new List<GateWay>();
            activity.Tasks ??= new List<IoTSharp.Models.Rule.BaseTask>();
            activity.LaneSet ??= new List<BpmnBaseObject>();
            activity.EndEvents ??= new List<BpmnBaseObject>();
            activity.StartEvents ??= new List<BpmnBaseObject>();
            activity.Containers ??= new List<BpmnBaseObject>();
            activity.BaseBpmnObjects ??= new List<BpmnBaseObject>();
            activity.DataStoreReferences ??= new List<BpmnBaseObject>();
            activity.SubProcesses ??= new List<BpmnBaseObject>();
            activity.DataOutputAssociations ??= new List<BpmnBaseObject>();
            activity.DataInputAssociations ??= new List<BpmnBaseObject>();
            activity.Lane ??= new List<BpmnBaseObject>();
            activity.TextAnnotations ??= new List<BpmnBaseObject>();
            activity.RuleId = id;
            var flows = _context.Flows.Where(c => c.FlowRule.RuleId == id && c.FlowStatus > 0 && c.Tenant.Id == profile.Tenant).ToList();
            activity.Xml = ruleflow.DefinitionsXml?.Trim('\r');
            foreach (var item in flows)
            {
                switch (item.FlowType)
                {
                    case "bpmn:SequenceFlow":

                        activity.SequenceFlows.Add(
                            new SequenceFlow()
                            {
                                sourceId = item.SourceId,
                                targetId = item.TargetId,
                                id = item.bpmnid,
                                bpmntype = item.FlowType,
                                BizObject = new FormBpmnObject
                                {
                                    Flowid = item.FlowId.ToString(),
                                    Flowdesc = item.Flowdesc,
                                    Flowtype = item.FlowType,
                                    Flowname = item.Flowname,
                                    conditionexpression = item.Conditionexpression
                                }
                            });
                        break;

                    case "bpmn:EndEvent":
                        activity.EndEvents.Add(

                            new GateWay
                            {
                                sourceId = item.SourceId,
                                id = item.bpmnid,
                                bpmntype = item.FlowType,
                                BizObject = new FormBpmnObject
                                {
                                    Flowid = item.FlowId.ToString(),
                                    Flowdesc = item.Flowdesc,
                                    Flowtype = item.FlowType,
                                    Flowname = item.Flowname,
                                    NodeProcessClass = item.NodeProcessClass,
                                }
                            });
                        break;

                    case "bpmn:StartEvent":
                        activity.StartEvents.Add(

                            new GateWay
                            {
                                sourceId = item.SourceId,

                                id = item.bpmnid,
                                bpmntype = item.FlowType,
                                BizObject = new FormBpmnObject
                                {
                                    Flowid = item.FlowId.ToString(),
                                    Flowdesc = item.Flowdesc,
                                    Flowtype = item.FlowType,
                                    Flowname = item.Flowname,
                                }
                            });

                        break;

                    case "bpmn:ExclusiveGateway":
                        activity.GateWays.Add(

                            new GateWay
                            {
                                sourceId = item.SourceId,

                                id = item.bpmnid,
                                bpmntype = item.FlowType,
                                BizObject = new FormBpmnObject
                                {
                                    Flowid = item.FlowId.ToString(),
                                    Flowdesc = item.Flowdesc,
                                    Flowtype = item.FlowType,
                                    Flowname = item.Flowname,
                                    NodeProcessClass = item.NodeProcessClass
                                }
                            });
                        break;

                    case "bpmn:ParallelGateway":
                        activity.GateWays.Add(

                            new GateWay
                            {
                                sourceId = item.SourceId,

                                id = item.bpmnid,
                                bpmntype = item.FlowType,
                                BizObject = new FormBpmnObject
                                {
                                    Flowid = item.FlowId.ToString(),
                                    Flowdesc = item.Flowdesc,
                                    Flowtype = item.FlowType,
                                    Flowname = item.Flowname,
                                    NodeProcessClass = item.NodeProcessClass
                                }
                            });
                        break;

                    case "bpmn:InclusiveGateway":
                        activity.GateWays.Add(

                            new GateWay
                            {
                                sourceId = item.SourceId,

                                id = item.bpmnid,
                                bpmntype = item.FlowType,
                                BizObject = new FormBpmnObject
                                {
                                    Flowid = item.FlowId.ToString(),
                                    Flowdesc = item.Flowdesc,
                                    Flowtype = item.FlowType,
                                    Flowname = item.Flowname,
                                    NodeProcessClass = item.NodeProcessClass
                                }
                            });
                        break;

                    case "bpmn:EventBasedGateway":
                        activity.GateWays.Add(

                            new GateWay
                            {
                                sourceId = item.SourceId,

                                id = item.bpmnid,
                                bpmntype = item.FlowType,
                                BizObject = new FormBpmnObject
                                {
                                    Flowid = item.FlowId.ToString(),
                                    Flowdesc = item.Flowdesc,
                                    Flowtype = item.FlowType,
                                    Flowname = item.Flowname,
                                    NodeProcessClass = item.NodeProcessClass
                                }
                            });
                        break;

                    case "bpmn:ComplexGateway":
                        activity.GateWays.Add(

                            new GateWay
                            {
                                sourceId = item.SourceId,

                                id = item.bpmnid,
                                bpmntype = item.FlowType,
                                BizObject = new FormBpmnObject
                                {
                                    Flowid = item.FlowId.ToString(),
                                    Flowdesc = item.Flowdesc,
                                    Flowtype = item.FlowType,
                                    Flowname = item.Flowname,
                                    NodeProcessClass = item.NodeProcessClass
                                }
                            });
                        break;

                    case "bpmn:Task":
                        activity.Tasks.Add(

                            new IoTSharp.Models.Rule.BaseTask
                            {
                                id = item.bpmnid,
                                Flowname = item.Flowname,
                                bpmntype = item.FlowType,
                                BizObject = new FormBpmnObject
                                {
                                    Flowid = item.FlowId.ToString(),
                                    Flowdesc = item.Flowdesc,
                                    Flowtype = item.FlowType,
                                    Flowname = item.Flowname,
                                    flowscript = item.NodeProcessScript,
                                    flowscripttype = item.NodeProcessScriptType,
                                    NodeProcessClass = item.NodeProcessClass,
                                    NodeProcessParams = item.NodeProcessParams
                                }
                            });
                        break;

                    case "bpmn:BusinessRuleTask":
                        activity.Tasks.Add(

                            new IoTSharp.Models.Rule.BaseTask
                            {
                                Flowname = item.Flowname,
                                id = item.bpmnid,
                                bpmntype = item.FlowType,
                                BizObject = new FormBpmnObject
                                {
                                    Flowid = item.FlowId.ToString(),
                                    Flowdesc = item.Flowdesc,
                                    Flowtype = item.FlowType,
                                    Flowname = item.Flowname,
                                    flowscript = item.NodeProcessScript,
                                    flowscripttype = item.NodeProcessScriptType,
                                    NodeProcessClass = item.NodeProcessClass
                                }
                            });
                        break;

                    case "bpmn:ReceiveTask":
                        activity.Tasks.Add(

                            new IoTSharp.Models.Rule.BaseTask
                            {
                                Flowname = item.Flowname,
                                id = item.bpmnid,
                                bpmntype = item.FlowType,
                                BizObject = new FormBpmnObject
                                {
                                    Flowid = item.FlowId.ToString(),
                                    Flowdesc = item.Flowdesc,
                                    Flowtype = item.FlowType,
                                    Flowname = item.Flowname,
                                    NodeProcessClass = item.NodeProcessClass
                                }
                            });
                        break;

                    case "bpmn:UserTask":
                        activity.Tasks.Add(

                            new IoTSharp.Models.Rule.BaseTask
                            {
                                Flowname = item.Flowname,
                                id = item.bpmnid,
                                bpmntype = item.FlowType,
                                BizObject = new FormBpmnObject
                                {
                                    Flowid = item.FlowId.ToString(),
                                    Flowdesc = item.Flowdesc,
                                    Flowtype = item.FlowType,
                                    Flowname = item.Flowname,
                                    flowscript = item.NodeProcessScript,
                                    flowscripttype = item.NodeProcessScriptType,
                                    NodeProcessClass = item.NodeProcessClass
                                }
                            });
                        break;

                    case "bpmn:ServiceTask":
                        activity.Tasks.Add(

                            new IoTSharp.Models.Rule.BaseTask
                            {
                                Flowname = item.Flowname,
                                id = item.bpmnid,
                                bpmntype = item.FlowType,
                                BizObject = new FormBpmnObject
                                {
                                    Flowid = item.FlowId.ToString(),
                                    Flowdesc = item.Flowdesc,
                                    Flowtype = item.FlowType,
                                    Flowname = item.Flowname,
                                    flowscript = item.NodeProcessScript,
                                    flowscripttype = item.NodeProcessScriptType,
                                    NodeProcessClass = item.NodeProcessClass
                                }
                            });
                        break;

                    case "bpmn:ManualTask":
                        activity.Tasks.Add(

                            new IoTSharp.Models.Rule.BaseTask
                            {
                                Flowname = item.Flowname,
                                id = item.bpmnid,
                                bpmntype = item.FlowType,
                                BizObject = new FormBpmnObject
                                {
                                    Flowid = item.FlowId.ToString(),
                                    Flowdesc = item.Flowdesc,
                                    Flowtype = item.FlowType,
                                    Flowname = item.Flowname,
                                    flowscript = item.NodeProcessScript,
                                    flowscripttype = item.NodeProcessScriptType,
                                    NodeProcessClass = item.NodeProcessClass
                                }
                            });
                        break;

                    case "bpmn:SendTask":
                        activity.Tasks.Add(

                            new IoTSharp.Models.Rule.BaseTask
                            {
                                Flowname = item.Flowname,
                                id = item.bpmnid,
                                bpmntype = item.FlowType,
                                BizObject = new FormBpmnObject
                                {
                                    Flowid = item.FlowId.ToString(),
                                    Flowdesc = item.Flowdesc,
                                    Flowtype = item.FlowType,
                                    Flowname = item.Flowname,
                                    flowscript = item.NodeProcessScript,
                                    flowscripttype = item.NodeProcessScriptType,
                                    NodeProcessClass = item.NodeProcessClass
                                }
                            });
                        break;

                    case "bpmn:CallActivity":
                        activity.Tasks.Add(

                            new IoTSharp.Models.Rule.BaseTask
                            {
                                Flowname = item.Flowname,
                                id = item.bpmnid,
                                bpmntype = item.FlowType,
                                BizObject = new FormBpmnObject
                                {
                                    Flowid = item.FlowId.ToString(),
                                    Flowdesc = item.Flowdesc,
                                    Flowtype = item.FlowType,
                                    Flowname = item.Flowname,
                                }
                            });
                        break;

                    case "bpmn:IntermediateCatchEvent":
                        activity.EndEvents.Add(

                            new BpmnBaseObject
                            {
                                Flowname = item.Flowname,
                                id = item.bpmnid,
                                bpmntype = item.FlowType,
                                BizObject = new FormBpmnObject
                                {
                                    Flowid = item.FlowId.ToString(),
                                    Flowdesc = item.Flowdesc,
                                    Flowtype = item.FlowType,
                                    Flowname = item.Flowname,
                                }
                            });
                        break;

                    case "bpmn:IntermediateThrowEvent":
                        activity.EndEvents.Add(

                            new BpmnBaseObject()
                            {
                                Flowname = item.Flowname,
                                id = item.bpmnid,
                                bpmntype = item.FlowType,
                                BizObject = new FormBpmnObject
                                {
                                    Flowid = item.FlowId.ToString(),
                                    Flowdesc = item.Flowdesc,
                                    Flowtype = item.FlowType,
                                    Flowname = item.Flowname,
                                }
                            });
                        break;

                    case "bpmn:Lane":
                        activity.Containers.Add(

                            new BpmnBaseObject
                            {
                                Flowname = item.Flowname,
                                id = item.bpmnid,
                                bpmntype = item.FlowType,
                                BizObject = new FormBpmnObject
                                {
                                    Flowid = item.FlowId.ToString(),
                                    Flowdesc = item.Flowdesc,
                                    Flowtype = item.FlowType,
                                    Flowname = item.Flowname,
                                }
                            });
                        break;

                    case "bpmn:Participant":
                        activity.Containers.Add(

                            new BpmnBaseObject
                            {
                                Flowname = item.Flowname,
                                id = item.bpmnid,
                                bpmntype = item.FlowType,
                                BizObject = new FormBpmnObject
                                {
                                    Flowid = item.FlowId.ToString(),
                                    Flowdesc = item.Flowdesc,
                                    Flowtype = item.FlowType,
                                    Flowname = item.Flowname,
                                }
                            });
                        break;

                    case "bpmn:DataStoreReference":
                        activity.DataStoreReferences.Add(

                            new BpmnBaseObject
                            {
                                Flowname = item.Flowname,
                                id = item.bpmnid,
                                bpmntype = item.FlowType,
                                BizObject = new FormBpmnObject
                                {
                                    Flowid = item.FlowId.ToString(),
                                    Flowdesc = item.Flowdesc,
                                    Flowtype = item.FlowType,
                                    Flowname = item.Flowname,
                                }
                            });
                        break;

                    case "bpmn:SubProcess":
                        activity.SubProcesses.Add(

                            new BpmnBaseObject
                            {
                                Flowname = item.Flowname,
                                id = item.bpmnid,
                                bpmntype = item.FlowType,
                                BizObject = new FormBpmnObject
                                {
                                    Flowid = item.FlowId.ToString(),
                                    Flowdesc = item.Flowdesc,
                                    Flowtype = item.FlowType,
                                    Flowname = item.Flowname,
                                }
                            });
                        break;

                    default:
                        BpmnBaseObject node = new BpmnBaseObject
                        {
                            BizObject = new FormBpmnObject
                            {
                                Flowid = item.FlowId.ToString(),
                                Flowdesc = item.Flowdesc,
                                Flowtype = item.FlowType,
                                Flowname = item.Flowname,
                            },
                            bpmntype = item.FlowType,
                            id = item.FlowId.ToString(),
                        };
                        activity.DefinitionsDesc = ruleflow.RuleDesc;
                        activity.RuleId = ruleflow.RuleId;
                        activity.DefinitionsName = ruleflow.Name;
                        activity.DefinitionsStatus = ruleflow.RuleStatus ?? 0;

                        activity.BaseBpmnObjects ??= new List<BpmnBaseObject>();
                        activity.BaseBpmnObjects.Add(node);
                        break;
                }
            }
            return new ApiResult<IoTSharp.Models.Rule.Activity>(ApiCode.Success, "Ok", activity);
        }

        [HttpPost]
        public async Task<ApiResult<dynamic>> Active([FromBody] JObject form)
        {
            var profile = this.GetUserProfile();
            var formdata = form.First.First;
            var extradata = form.First.Next;
            var obj = extradata.First.First.First.Value<JToken>();
            var __ruleid = obj.Value<string>();
            var ruleid = Guid.Parse(__ruleid);

            var d = formdata.Value<JToken>().ToObject<object>();

            var testabizId = Guid.NewGuid().ToString(); //根据业务保存起来，用来查询执行事件和步骤
            var result = await _flowRuleProcessor.RunFlowRules(ruleid, d, Guid.Empty, FlowRuleRunType.TestPurpose, testabizId);
            _context.SaveFlowResult(Guid.Empty, ruleid, result);
            return new ApiResult<dynamic>(ApiCode.Success, "test complete", result.OrderBy(c => c.Step).
                Where(c => c.BaseEvent.Bizid == testabizId).ToList()
                .GroupBy(c => c.Step).Select(c => new
                {
                    Step = c.Key,
                    Nodes = c
                }).ToList());
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="m"></param>
        /// <returns></returns>

        [HttpPost]
        public async Task<ApiResult<PagedData<BaseEventDto>>> FlowEvents([FromBody] EventParam m)
        {
            var profile = this.GetUserProfile();
            Expression<Func<BaseEvent, bool>> condition = x => x.EventStaus > -1 && x.Tenant.Id == profile.Tenant;
            if (!string.IsNullOrEmpty(m.Name))
            {
                condition = condition.And(x => x.EventName.Contains(m.Name));
            }

            if (m.CreatTime != null && m.CreatTime.Length == 2)
            {
                condition = condition.And(x => x.CreaterDateTime > m.CreatTime[0] && x.CreaterDateTime < m.CreatTime[1]);
            }

            if (m.RuleId != null)
            {
                condition = condition.And(x => x.FlowRule.RuleId == m.RuleId);
            }

            if (m.Creator != null && m.Creator != Guid.Empty)
            {
                condition = condition.And(x => x.Creator == m.Creator.Value);
            }

            var result = _context.BaseEvents.OrderByDescending(c => c.CreaterDateTime).Where(condition)
                .Skip((m.Offset) * m.Limit).Take(m.Limit).Select(c => new BaseEventDto
                {
                    Name = c.FlowRule.Name,
                    Bizid = c.Bizid,
                    CreaterDateTime = c.CreaterDateTime,
                    Creator = c.Creator,
                    EventDesc = c.EventDesc,
                    EventId = c.EventId,
                    EventStaus = c.EventStaus,
                    EventName = c.EventName,
                    MataData = c.MataData,
                    RuleId = c.FlowRule.RuleId,
                    Type = c.Type
                }).ToList();

            foreach (var item in result)
            {
                item.CreatorName = await GetCreatorName(item);
            }
            return new ApiResult<PagedData<BaseEventDto>>(ApiCode.Success, "OK", new PagedData<BaseEventDto>
            {
                total = _context.BaseEvents.Count(condition),
                rows = result
            });
        }

        private async Task<string> GetCreatorName(BaseEventDto dto)
        {
            if (dto.Type == FlowRuleRunType.Normal)
            {
                return _context.Device.SingleOrDefault(c => c.Id == dto.Creator)?.Name;
            }
            else
            {
                return (await _userManager.FindByIdAsync(dto.Creator.ToString()))?.UserName;
            }
        }

        [HttpGet]
        public ApiResult<dynamic> GetFlowOperations(Guid eventId)
        {
            var profile = this.GetUserProfile();
            var _event = _context.BaseEvents.Include(c => c.FlowRule).SingleOrDefault(c => c.EventId == eventId);
            var _operations = _context.FlowOperations.Include(c => c.Flow).Where(c => c.BaseEvent == _event).ToList();

            var flows = _context.Flows.Where(c => c.FlowRule.RuleId == _event.FlowRule.RuleId);
            var sf = flows.Where(c => c.FlowType == "bpmn:SequenceFlow").ToArray();
            var links = new List<dynamic>();
            var nodes = new List<string>();
            foreach (var item in sf)
            {
                var target = _operations.FirstOrDefault(c => c.Flow.bpmnid == item.TargetId);
                var source = _operations.FirstOrDefault(c => c.Flow.bpmnid == item.SourceId);
                if (target != null && source != null)
                {
                    links.Add(new { source = source.Flow.Flowname ?? source.bpmnid, target = target.Flow.Flowname ?? target.bpmnid, value = (target.AddDate - source.AddDate).Value.TotalMilliseconds });
                    var _sourcename = source.Flow.Flowname ?? source.bpmnid;
                    var _targetname = target.Flow.Flowname ?? target.bpmnid;
                    if (nodes.All(c => c != _sourcename))
                    {
                        nodes.Add(_sourcename);
                    }
                    if (nodes.All(c => c != _targetname))
                    {
                        nodes.Add(_targetname);
                    }
                }
            }
            var steps = _operations.OrderBy(c => c.Step).
                ToList()
                .GroupBy(c => c.Step).Select(c => new
                {
                    Step = c.Key,
                    Nodes = c
                }).ToList();
            return new ApiResult<dynamic>(ApiCode.Success, "OK", new
            {
                steps,
                charts = new
                {
                    sankey = new { links, nodes = nodes.Select(c => new { name = c }).ToList() }
                }
            });
        }

        [HttpGet]
        public ApiResult<dynamic> GetExecutors()
        {
            return new ApiResult<dynamic>(ApiCode.Success, "OK", _helper.GetTaskExecutorList().Select(c => new { label = c.Key, value = c.Value.FullName }).ToList());
        }

        [HttpPost]
        public async Task<ApiResult<PagedData<RuleTaskExecutor>>> Executors(QueryDto m)
        {
            var profile = this.GetUserProfile();
            var rte = from x in _context.RuleTaskExecutors where x.ExecutorStatus > -1 && x.Tenant.Id == profile.Tenant orderby x.AddDateTime descending select x;
            var pd = new PagedData<RuleTaskExecutor>
            {
                total = await rte.CountAsync(),
                rows = await rte.Skip((m.Offset) * m.Limit).Take(m.Limit).ToListAsync()
            };
            return new ApiResult<PagedData<RuleTaskExecutor>>(ApiCode.Success, "OK", pd);
        }

        [HttpGet]
        public async Task<ApiResult<RuleTaskExecutor>> GetExecutor(Guid Id)
        {
            var profile = this.GetUserProfile();
            var executor = await _context.RuleTaskExecutors.SingleOrDefaultAsync(c => c.ExecutorId == Id && c.Tenant.Id == profile.Tenant);

            if (executor != null)
            {
                return new ApiResult<RuleTaskExecutor>(ApiCode.Success, "Ok", executor);
            }
            return new ApiResult<RuleTaskExecutor>(ApiCode.CantFindObject, "cant't find that object", null);
        }

        [HttpGet]
        public async Task<ApiResult<bool>> DeleteExecutor(Guid Id)
        {
            var profile = this.GetUserProfile();
            var executor = await _context.RuleTaskExecutors.SingleOrDefaultAsync(c => c.ExecutorId == Id && c.Tenant.Id == profile.Tenant);

            if (executor != null)
            {
                executor.ExecutorStatus = -1;
                _context.RuleTaskExecutors.Update(executor);
                await _context.SaveChangesAsync();
                return new ApiResult<bool>(ApiCode.Success, "Ok", true);
            }
            return new ApiResult<bool>(ApiCode.CantFindObject, "cant't find that object", false);
        }

        [HttpPost]
        public async Task<ApiResult<bool>> UpdateExecutor(RuleTaskExecutor m)
        {
            var profile = this.GetUserProfile();
            var executor = await _context.RuleTaskExecutors.SingleOrDefaultAsync(c => c.ExecutorId == m.ExecutorId && c.Tenant.Id == profile.Tenant);
            if (executor != null)
            {
                executor.DefaultConfig = m.DefaultConfig;
                executor.ExecutorDesc = m.ExecutorDesc;
                executor.ExecutorName = m.ExecutorName;
                executor.TypeName = m.ExecutorName;
                executor.Path = m.Path;
                executor.Tag = m.Tag;
                _context.RuleTaskExecutors.Update(executor);
                await _context.SaveChangesAsync();
                return new ApiResult<bool>(ApiCode.Success, "Ok", true);
            }
            return new ApiResult<bool>(ApiCode.CantFindObject, "cant't find that object", false);
        }

        [HttpPost]
        public async Task<ApiResult<bool>> AddExecutor(RuleTaskExecutor m)
        {
            var profile = this.GetUserProfile();
            var executor = new RuleTaskExecutor();
            executor.DefaultConfig = m.DefaultConfig;
            executor.ExecutorDesc = m.ExecutorDesc;
            executor.ExecutorName = m.ExecutorName;
            executor.TypeName = m.ExecutorName;
            executor.Path = m.Path;
            executor.Tag = m.Tag;
            executor.AddDateTime = DateTime.UtcNow;
            executor.Creator = User.GetUserId();
            executor.ExecutorStatus = 1;
            _context.JustFill(this, executor);
            _context.RuleTaskExecutors.Add(executor);
            var rest = await _context.SaveChangesAsync();
            return new ApiResult<bool>(ApiCode.Success, "Ok", rest > 0);
        }

        [HttpPost]
        public async Task<ApiResult<RuleTaskExecutorTestResultDto>> TestTask(RuleTaskExecutorTestDto m)
        {
            var profile = this.GetUserProfile();
            var result = await _flowRuleProcessor.TestScript(m.ruleId, m.flowId, m.Data);
            await _context.SaveChangesAsync();
            return new ApiResult<RuleTaskExecutorTestResultDto>(ApiCode.Success, "Ok", new RuleTaskExecutorTestResultDto() { Data = result.Data });
        }

        [HttpPost("RuleCondition")]
        public async Task<ApiResult<ConditionTestResult>> RuleCondition([FromBody] RuleTaskFlowTestResultDto m)
        {
            var profile = this.GetUserProfile();
            var data = JsonConvert.DeserializeObject(m.Data) as JObject;
            var d = data.ToObject(typeof(ExpandoObject));
            var result = await _flowRuleProcessor.TestCondition(m.ruleId, m.flowId, d);
            return new ApiResult<ConditionTestResult>(ApiCode.Success, "Ok", result);
        }
    }
}