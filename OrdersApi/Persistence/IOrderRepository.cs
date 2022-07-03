﻿using System;
using System.Threading.Tasks;
using OrdersApi.Models;

namespace OrdersApi.Persistence
{
    public interface IOrderRepository
    {
        public Task<Order> GetOrderAsync(Guid id);
        public Task RegisterOrder(Order order);
        //public Task UpdateOrder(Order order);

    }
}
