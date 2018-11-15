using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Sitecore.Data.Items;
using Sitecore.DataExchange.Contexts;
using Sitecore.DataExchange.Extensions;
using Sitecore.DataExchange.Local.Commands;
using Sitecore.DataExchange.Local.Runners;
using Sitecore.DataExchange.Models;
using Sitecore.DataExchange.Plugins;
using Sitecore.Globalization;
using Sitecore.Shell.Framework.Commands;

namespace Sitecore.Support.DataExchange.Local.Commands
{
  [Serializable]
  public class ShowPipelineBatchStatusCommand: BasePipelineBatchCommand

  {
    private const int DefaultRefreshTimer = 2;

    private PipelineBatchContext _pipelineBatchContext;

    private PipelineBatch _pipelineBatch;
    public override void Execute(CommandContext context)
    {
      if (base.PipelineBatchRunner != null && context?.Items != null && context.Items.Length != 0)
      {
        Item item = context.Items[0];
        _pipelineBatch = GetPipelineBatch(item);
        InProcessPipelineBatchRunner inProcessPipelineBatchRunner = (InProcessPipelineBatchRunner)base.PipelineBatchRunner;
        if (!IsPipelineBatchRunning(item) || _pipelineBatch == null || inProcessPipelineBatchRunner == null)
        {
          Context.ClientPage.ClientResponse.Alert(Translate.Text("Cannot get the pipeline batch status because the pipeline batch is not currently running."));
          Context.ClientPage.ClientResponse.Timer($"item:load(id={item.ID})", 0);
        }
        else
        {
          int delay = 0;
          if (!int.TryParse(context.Parameters["refreshTimer"], out delay))
          {
            delay = 2;
          }
          if (inProcessPipelineBatchRunner.IsRunningRemotely(_pipelineBatch))
          {
            string text = inProcessPipelineBatchRunner.GetServerName(_pipelineBatch);
            if (text == null)
            {
              text = string.Empty;
            }
            Context.ClientPage.ClientResponse.Alert(Translate.Text("{0} is running on {1} server.", item.Name, text));
            Context.ClientPage.ClientResponse.Timer($"item:load(id={item.ID})", delay);
          }
          else
          {
            string text2 = string.Empty;
            PipelineBatchSummary pipelineBatchSummary = inProcessPipelineBatchRunner.GetRunningPipelineBatchContext(_pipelineBatch.Identifier)?.CurrentPipelineBatch?.GetPipelineBatchSummary();
            DateTime dateTime;
            if (pipelineBatchSummary != null)
            {
              dateTime = pipelineBatchSummary.RequestedAt;
              text2 = Environment.NewLine + Translate.Text("Entities submitted: {0}", pipelineBatchSummary.EntitySubmitedCount);
              foreach (KeyValuePair<string, object> status in pipelineBatchSummary.Statuses)
              {
                text2 = text2 + Environment.NewLine + status.Key + ": " + status.Value;
              }
            }
            else
            {
              dateTime = DateTime.Now;
            }
            TimeSpan timeSpan = DateTime.UtcNow - dateTime;
            string ft = string.Format("{0}:{1:00}:{2:00}", (int)timeSpan.TotalHours, timeSpan.Minutes, timeSpan.Seconds);
            string str = Translate.Text("{0} started at {1} and is running on {2} server for {3}.", item.Name, DateUtil.FormatShortDateTime(dateTime), "local",ft);
            str += text2;
            Context.ClientPage.ClientResponse.Alert(str);
            Context.ClientPage.ClientResponse.Timer($"item:load(id={item.ID})", 2);
          }
        }
      }
    }

    protected override bool ShouldEnableButton(CommandContext context)
    {
      if (base.ShouldEnableButton(context) && IsPipelineBatchRunning(context.Items[0]))
      {
        return true;
      }
      return false;
    }
  }
}