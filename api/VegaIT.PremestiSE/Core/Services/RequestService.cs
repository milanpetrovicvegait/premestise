﻿using Core.Interfaces.Intefaces;
using Core.Interfaces.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Transactions;
using Core.Services.Mappers;
using Persistence.Interfaces.Contracts;
using Persistence.Interfaces.Entites;

namespace Core.Services
{
    public class RequestService : IRequestService
    {

        private readonly IPendingRequestRepository _pendingRequestRepository;
        private readonly IMatchedRequestRepository _matchedRequestRepository;
        private readonly IPendingRequestWishRepository _requestWishRepository;
        private readonly IKindergardenRepository _kindergardenRepository;

        public RequestService(IPendingRequestRepository pendingRequestRepository, IKindergardenRepository kindergardenRepository, IPendingRequestWishRepository requestWishRepository, IMatchedRequestRepository matchedRequestRepository)

        {
            _pendingRequestRepository = pendingRequestRepository;
            _kindergardenRepository = kindergardenRepository;
            _requestWishRepository = requestWishRepository;
            _matchedRequestRepository = matchedRequestRepository;
        }

        public RequestDto CreatePending(RequestDto newRequest)
        {   //Kad se kreira pending request treba da se kreira i entry u pending_request_wishes
            var requestMapper = new RequestMapper();

            if (newRequest.ToKindergardenIds == null)
                newRequest.ToKindergardenIds = new List<int>(0);
            var pendingRequestToAdd = requestMapper.DtoToEntity(newRequest);

            var addedPendingRequest = _pendingRequestRepository.Create(pendingRequestToAdd as PendingRequest);

            return requestMapper.DtoFromEntity(addedPendingRequest);
        }

        public void DeletePending(int id)
        {
            _pendingRequestRepository.Delete(id);
        }

        public RequestDto CreateMatched(RequestDto newRequest)
        {
            var requestMapper = new RequestMapper();

            if (newRequest.ToKindergardenIds == null)
                newRequest.ToKindergardenIds = new List<int>(0);
            var matchedRequestToAdd = requestMapper.DtoToEntity(newRequest);

            var addedPendingRequest = _matchedRequestRepository.Create(matchedRequestToAdd as MatchedRequest);

            return requestMapper.DtoFromEntity(addedPendingRequest);
        }

        public void DeleteMatched(int id)
        {
            _matchedRequestRepository.Delete(id);
        }

        public IEnumerable<RequestDto> GetAllMatched()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<RequestDto> GetAllPending()
        {
            throw new NotImplementedException();
        }
    }
}
