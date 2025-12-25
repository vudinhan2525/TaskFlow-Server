import { Injectable } from '@nestjs/common';
import { ConfigService } from '@nestjs/config';
import { IMailSender, SendMailOptions } from '@notification-service/core/interfaces/mail-sender.interface';
import * as nodemailer from 'nodemailer';
import * as fs from 'fs';
import * as handlebars from 'handlebars';
import { join } from 'path';

@Injectable()
export class MailSenderRepo implements IMailSender {
  private transporter: nodemailer.Transporter;

  constructor(private readonly configService: ConfigService) {
    this.transporter = nodemailer.createTransport({
      host: this.configService.get<string>('MAIL_SMTP_HOST', 'smtp.mailtrap.io'),
      port: this.configService.get<number>('MAIL_SMTP_PORT', 587),
      secure: false,
      auth: {
        user: this.configService.get<string>('MAIL_SMTP_USER'),
        pass: this.configService.get<string>('MAIL_SMTP_PASS'),
      },
    });
  }

  async sendMail(options: SendMailOptions): Promise<void> {
    let html = options.html;

    if (options.template && options.data) {
      const templatePath = join(__dirname, 'public', 'templates', `${options.template}`);
      console.log(`Template path resolved to: ${templatePath}`);
      const template = fs.readFileSync(templatePath, 'utf8');
      const compiled = handlebars.compile(template);
      html = compiled(options.data);
    }

    console.log('Attempting to send email with options:', options);
    await this.transporter.sendMail({
      from: `"My App" <${this.configService.get<string>('MAIL_SMTP_USER')}>`,
      to: options.to,
      subject: options.subject,
      text: options.text,
      html,
    });
  }
}
