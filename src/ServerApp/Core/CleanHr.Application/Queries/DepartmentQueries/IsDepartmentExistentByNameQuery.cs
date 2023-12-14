﻿using CleanHr.Domain.Aggregates.DepartmentAggregate;
using MediatR;
using TanvirArjel.ArgumentChecker;
using TanvirArjel.EFCore.GenericRepository;

namespace CleanHr.Application.Queries.DepartmentQueries;

public sealed class IsDepartmentExistentByNameQuery(string name) : IRequest<bool>
{

    public string Name { get; set; } = name.ThrowIfNullOrEmpty(nameof(name));

    private class IsDepartmentExistentByNameQueryHandler(IQueryRepository repository) : IRequestHandler<IsDepartmentExistentByNameQuery, bool>
    {

        public async Task<bool> Handle(IsDepartmentExistentByNameQuery request, CancellationToken cancellationToken)
        {
            request.ThrowIfNull(nameof(request));
            bool isExists = await repository.ExistsAsync<Department>(d => d.Name.Value == request.Name, cancellationToken);
            return isExists;
        }
    }
}
