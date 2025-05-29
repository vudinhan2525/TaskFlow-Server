using System.Threading.Tasks;
using MainService.Domain.Entities;

namespace MainService.Domain.Interfaces;

public interface IPublisherService
{
    Task Emit<T>(T message);
    Task PublishActivity(ActivityDomain activity);
}