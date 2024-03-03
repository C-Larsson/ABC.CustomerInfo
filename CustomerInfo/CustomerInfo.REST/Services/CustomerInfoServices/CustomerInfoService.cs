﻿using CustomerInfo.REST.Data;
using CustomerInfo.REST.Models;
using Microsoft.EntityFrameworkCore;

namespace CustomerInfo.REST.Services.CustomerInfoServices
{
    public class CustomerInfoService : ICustomerInfoService
    {

        private readonly AppDbContext _dbContext;

        public CustomerInfoService(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<Customer?> GetBySsn(string ssn)
        {
            return await _dbContext.Customers.FindAsync(ssn);
        }

        public async Task<Customer> Create(Customer customer)
        {
            TransformPhoneIfNeeded(customer);
            _dbContext.Customers.Add(customer);
            await _dbContext.SaveChangesAsync();
            return customer;
        }

        public async Task<Customer> Update(Customer customer)
        {
            TransformPhoneIfNeeded(customer);
            var customerDB = await _dbContext.Customers.FindAsync(customer.SSN);

            // Only update if not null
            if (customer.Email != null) customerDB.Email = customer.Email;
            if (customer.PhoneNumber != null) customerDB.PhoneNumber = customer.PhoneNumber;

            await _dbContext.SaveChangesAsync();
            return customerDB;
        }

        public async Task<bool> Delete(string ssn)
        {
            var customer = await _dbContext.Customers.FindAsync(ssn);
            if (customer != null)
            {
                _dbContext.Customers.Remove(customer);
                await _dbContext.SaveChangesAsync();
                return true;
            }
            return false;
        }

        public async Task<List<Customer>> GetUsers()
        {
            return await _dbContext.Customers.ToListAsync();
        }

        public async Task<CustomerSearchResult> SearchCustomers(string searchText, int page)
        {
            var pageResults = 2f;
            var pageCount = Math.Ceiling((await FindCustomersBySearchText(searchText)).Count / pageResults);

            var customers = await _dbContext.Customers
                .Where(c => c.SSN.ToLower().Contains(searchText.ToLower()) ||
                    c.Email.ToLower().Contains(searchText.ToLower()) ||
                    c.PhoneNumber.ToLower().Contains(searchText.ToLower()))
                .Skip((page - 1) * (int)pageResults)
                .Take((int)pageResults)
                .ToListAsync();
            
            return new CustomerSearchResult
            {
                Customers = customers,
                Pages = (int)pageCount,
                CurrentPage = page
            };

        }

        public async Task<List<string>> GetCustomerSearchSuggestions(string searchText)
        {
            var customers = await FindCustomersBySearchText(searchText);
            var result = new List<string>();

            foreach (var customer in customers)
            {
                if (customer.SSN.ToLower().Contains(searchText.ToLower()))
                    result.Add(customer.SSN);
                if (customer.Email.ToLower().Contains(searchText.ToLower()))
                    result.Add(customer.Email);
                if (customer.PhoneNumber.ToLower().Contains(searchText.ToLower()))
                    result.Add(customer.PhoneNumber);
            }
            return result;
        }

        private void TransformPhoneIfNeeded(Customer customer)
        {
            // Replace leading 0 with +46
            if (customer.PhoneNumber != null && customer.PhoneNumber.StartsWith("0"))
                customer.PhoneNumber = $"+46{customer.PhoneNumber.Remove(0, 1)}";
        }

        private async Task<List<Customer>> FindCustomersBySearchText(string searchText)
        {
            return await _dbContext.Customers
                .Where(c => c.SSN.ToLower().Contains(searchText.ToLower()) ||
                    c.Email.ToLower().Contains(searchText.ToLower()) ||
                    c.PhoneNumber.ToLower().Contains(searchText.ToLower())).ToListAsync();
        }

    }

}

