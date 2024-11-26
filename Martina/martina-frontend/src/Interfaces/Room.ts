export interface Room {
    id: string;
    name: string;
    userId:string;
    userName: string;
    status: 'occupied' | 'free'
}