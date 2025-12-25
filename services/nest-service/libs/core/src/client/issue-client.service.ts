import { IssueRes } from '@nest-service/core/types/base';
import { IssueServiceClient } from '@nest-service/core/types/main_service/issue';
import { Inject, Injectable, OnModuleInit } from '@nestjs/common';
import { ClientGrpc } from '@nestjs/microservices';
import { firstValueFrom } from 'rxjs';
export interface GetListIssuesClientParams {
  issueIds: string[];
  limit: number;
  page: number;
}
@Injectable()
export class IssueClientService implements OnModuleInit {
  private issueGrpcService: IssueServiceClient;

  constructor(@Inject('ISSUE_PACKAGE') private client: ClientGrpc) {}

  onModuleInit() {
    this.issueGrpcService = this.client.getService<IssueServiceClient>('IssueService');
  }

  async getListIssues(params: GetListIssuesClientParams): Promise<IssueRes[] | undefined> {
    const res = await firstValueFrom(
      this.issueGrpcService.listIssues({
        page: params.page,
        limit: params.limit,
        assigneeIds: [],
        columnIds: [],
        issueIds: params.issueIds,
        sprintIds: [],
        projectId: '',
        types: [],
        priorities: [],
      }),
    );
    return res.data;
  }
}
