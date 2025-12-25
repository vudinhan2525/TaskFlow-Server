// import { IoAdapter } from '@nestjs/platform-socket.io';
// import { ServerOptions } from 'socket.io';
// import { INestApplication } from '@nestjs/common';

// export class SocketIoAdapter extends IoAdapter {
//   constructor(app: INestApplication) {
//     super(app);
//   }

//   createIOServer(port: number, options?: ServerOptions) {
//     const server = super.createIOServer(port, {
//       ...options,
//       cors: {
//         origin: ['http://localhost:5173', 'http://localhost:5003'],
//         methods: ['GET', 'POST'],
//         credentials: true,
//       },
//     });
//     return server;
//   }
// }
