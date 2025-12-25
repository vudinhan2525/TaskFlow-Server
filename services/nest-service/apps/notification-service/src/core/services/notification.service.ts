import { Injectable } from '@nestjs/common';
import { NotificationDomain, NotificationType, ReferenceType } from '@notification-service/core/models/notification';
import { INotificationRepo } from '@notification-service/core/interfaces/notification-repo.interface';
import { RpcException } from '@nestjs/microservices';
import { status } from '@grpc/grpc-js';
import { IssueClientService, ProjectClientService, SprintClientService, UserClientService } from '@nest-service/core';
import { ProjectRes } from '@nest-service/core/types/main_service/project';
import { SprintRes } from '@nest-service/core/types/main_service/sprint';
import { IssueRes, UserRes } from '@nest-service/core/types/base';
import { NotificationEmitterService } from '@notification-service/adapters/websocket/notification.websocket';

export interface CreateNotificationParams {
  recipientId: string;
  actorId?: string;
  type: NotificationType;
  referenceId?: string;
  content: string;
  isRead: boolean;
  createdAt: Date;
}

export interface GetAllNotificationParams {
  userId?: string | undefined;
  projectId?: string | undefined;
  sprintId?: string | undefined;
  page?: number | undefined;
  limit?: number | undefined;
}

export interface UpdateNotificationParams {
  isRead?: boolean;
  notiId: string;
}

export interface BulkUpdateNotificationParams {
  userId?: string;
  isRead?: boolean;
}

@Injectable()
export class NotificationService {
  constructor(
    private readonly notificationRepo: INotificationRepo,
    private readonly projectClientService: ProjectClientService,
    private readonly sprintCLientService: SprintClientService,
    private readonly issueClientService: IssueClientService,
    private readonly userClientService: UserClientService,
    private readonly notiEmitter: NotificationEmitterService,
  ) {}

  async createNotification(data: CreateNotificationParams): Promise<NotificationDomain> {
    if (!data.referenceId) {
      throw new RpcException({
        code: status.INVALID_ARGUMENT,
        message: 'Missing referenceId for notification',
      });
    }

    const refType = this.getReferenceTypeByNotification(data.type);

    const noti = await this.notificationRepo.create({
      recipientId: data.recipientId,
      actorId: data.actorId,
      type: data.type,
      referenceId: data.referenceId,
      referenceType: refType,
      content: data?.content || '',
      isRead: false,
      createdAt: new Date(),
    });

    await this.notiEmitter.sendToUser(data.recipientId);

    return noti;
  }

  async listNotifications(params: GetAllNotificationParams): Promise<{ data: NotificationDomain[]; totalCount: number }> {
    const [notiDomain, totalCount] = await Promise.all([this.notificationRepo.listAll(params), this.notificationRepo.countAll(params)]);
    const issueIds: string[] = [];
    const projectIds: string[] = [];
    const sprintIds: string[] = [];
    const actorIds = notiDomain.map((noti) => noti?.actorId).filter(Boolean) as string[];

    notiDomain.forEach((noti) => {
      if (noti.referenceType == ReferenceType.ISSUE && noti.referenceId) {
        issueIds.push(noti.referenceId);
      }
      if (noti.referenceType == ReferenceType.PROJECT && noti.referenceId) {
        projectIds.push(noti.referenceId);
      }
      if (noti.referenceType == ReferenceType.SPRINT && noti.referenceId) {
        sprintIds.push(noti.referenceId);
      }
    });

    const [projects, sprints, issues, receivers, actors] = await Promise.all([
      this.projectClientService.getListProjects({ projectIds: projectIds, limit: params.limit || 100, page: params.page || 1 }),
      this.sprintCLientService.getListSprints({ sprintIds: sprintIds, limit: params.limit || 100, page: params.page || 1 }),
      this.issueClientService.getListIssues({ issueIds: issueIds, limit: params.limit || 100, page: params.page || 1 }),
      this.userClientService.getListUsers({ userIds: notiDomain.map((noti) => noti.recipientId), limit: params.limit || 100, page: params.page || 1 }),
      this.userClientService.getListUsers({ userIds: actorIds, limit: params.limit || 100, page: params.page || 1 }),
    ]);

    const projectsMap = new Map<string, ProjectRes>();
    (projects || []).forEach((project) => {
      projectsMap.set(project.id, project);
    });

    const sprintsMap = new Map<string, SprintRes>();
    (sprints || []).forEach((sprint) => {
      sprintsMap.set(sprint.id, sprint);
    });

    const issuesMaps = new Map<string, IssueRes>();
    (issues || []).forEach((issue) => {
      issuesMaps.set(issue.id, issue);
    });

    const receiverMaps = new Map<string, UserRes>();
    (receivers || []).forEach((receiver) => {
      receiverMaps.set(receiver.id, receiver);
    });

    const actorMaps = new Map<string, UserRes>();
    (actors || []).forEach((actor) => {
      actorMaps.set(actor.id, actor);
    });

    notiDomain.forEach((noti) => {
      if (noti.referenceType === ReferenceType.PROJECT && noti.referenceId) {
        const project = projectsMap.get(noti.referenceId);
        if (project) {
          noti.referenceData = project;
        }
      }
      if (noti.referenceType === ReferenceType.SPRINT && noti.referenceId) {
        const sprint = sprintsMap.get(noti.referenceId);
        if (sprint) {
          noti.referenceData = sprint;
        }
      }
      if (noti.referenceType === ReferenceType.ISSUE && noti.referenceId) {
        const issue = issuesMaps.get(noti.referenceId);
        if (issue) {
          noti.referenceData = issue;
        }
      }
      // ...
      const receiver = receiverMaps.get(noti.recipientId);
      if (receiver) {
        noti.recipient = receiver;
      }
      if (noti.actorId) {
        const actor = actorMaps.get(noti.actorId);
        noti.actor = actor;
      }
    });
    return {
      data: notiDomain,
      totalCount: totalCount,
    };
  }

  async updateNotification(data: UpdateNotificationParams): Promise<NotificationDomain> {
    const noti = await this.notificationRepo.update(data);
    return noti;
  }

  async bulkUpdateNotification(data: BulkUpdateNotificationParams): Promise<{ success: boolean }> {
    return await this.notificationRepo.bulkUpdate(data);
  }

  private getReferenceTypeByNotification(type: NotificationType): ReferenceType {
    switch (type) {
      case NotificationType.ASSIGNMENT:
      case NotificationType.MENTION:
      case NotificationType.COMMENT:
      case NotificationType.STATUS_UPDATE:
      case NotificationType.DUE_DATE_REMINDER:
        return ReferenceType.ISSUE;

      case NotificationType.SPRINT_STARTED:
        return ReferenceType.SPRINT;

      case NotificationType.PROJECT_INVITATION:
      case NotificationType.PROJECT_ADDED:
        return ReferenceType.PROJECT;

      case NotificationType.REACTION:
        return ReferenceType.COMMENT;

      case NotificationType.PROJECT_TEAM_ADDED:
        return ReferenceType.PROJECT_MEMBER;

      case NotificationType.SYSTEM_ALERT:
        return ReferenceType.SYSTEM;

      default:
        return ReferenceType.SYSTEM;
    }
  }

  ValidateNotificationType(type: string): NotificationType {
    if (!type || !Object.values(NotificationType).includes(type as NotificationType)) {
      throw new RpcException({
        code: status.INVALID_ARGUMENT,
        message: 'Invalid notification type',
      });
    }
    return type as NotificationType;
  }
}
