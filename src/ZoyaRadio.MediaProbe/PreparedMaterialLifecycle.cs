using System.Diagnostics.CodeAnalysis;

namespace ZoyaRadio.MediaProbe;

internal sealed class PreparedMaterialLifecycle
{
    public PreparedMaterialLifecycleResult Apply(
        PreparedMaterialLifecycleState state,
        PreparedMaterialLifecycleInput input)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(input);

        return input switch
        {
            PreparedMaterialRegister register => ApplyRegister(state, register),
            PreparedMaterialAttachPayload attach => ApplyAttachPayload(state, attach),
            PreparedMaterialReserve reserve => ApplyReserve(state, reserve),
            PreparedMaterialRelease release => ApplyRelease(state, release),
            PreparedMaterialComplete complete => ApplyComplete(state, complete),
            PreparedMaterialTerminate terminate => ApplyTerminate(state, terminate),
            PreparedMaterialEvaluateExpiry expiry => ApplyExpiry(state, expiry),
            PreparedMaterialInvalidateBinding invalidation =>
                ApplyBindingInvalidation(state, invalidation),
            _ => throw new ArgumentOutOfRangeException(nameof(input)),
        };
    }

    private static PreparedMaterialLifecycleResult ApplyRegister(
        PreparedMaterialLifecycleState state,
        PreparedMaterialRegister input)
    {
        EnsureMaterialId(input.MaterialId);
        ArgumentNullException.ThrowIfNull(input.Binding);
        ValidateBinding(input.ReusePolicy, input.Binding);
        if (input.PayloadHandle is not null)
        {
            EnsurePayloadHandle(input.PayloadHandle.Value);
        }

        if (state.Materials.ContainsKey(input.MaterialId))
        {
            var decisions = new List<PreparedMaterialDecision>
            {
                new(
                    PreparedMaterialDecisionReason.DuplicateRegistrationRefused,
                    input.MaterialId),
            };
            AddPayloadRefusal(decisions, input.MaterialId, input.PayloadHandle);
            return new PreparedMaterialLifecycleResult(state, decisions);
        }

        if (IsExpired(input.Binding, input.RegisteredAt))
        {
            var decisions = new List<PreparedMaterialDecision>
            {
                new(
                    PreparedMaterialDecisionReason.RegistrationExpiredRefused,
                    input.MaterialId),
            };
            AddPayloadRefusal(decisions, input.MaterialId, input.PayloadHandle);
            return new PreparedMaterialLifecycleResult(state, decisions);
        }

        PreparedMaterialRecord material;
        var accepted = new List<PreparedMaterialDecision>();
        if (input.PayloadHandle is null)
        {
            material = PreparedMaterialRecord.Preparing(
                input.MaterialId,
                input.ReusePolicy,
                input.Binding,
                input.RegisteredAt);
            accepted.Add(new PreparedMaterialDecision(
                PreparedMaterialDecisionReason.RegisteredPreparing,
                input.MaterialId));
        }
        else
        {
            material = PreparedMaterialRecord.Ready(
                input.MaterialId,
                input.ReusePolicy,
                input.Binding,
                input.RegisteredAt,
                input.PayloadHandle.Value);
            accepted.Add(new PreparedMaterialDecision(
                PreparedMaterialDecisionReason.RegisteredReady,
                input.MaterialId,
                input.PayloadHandle));
            accepted.Add(new PreparedMaterialDecision(
                PreparedMaterialDecisionReason.PayloadOwnershipAccepted,
                input.MaterialId,
                input.PayloadHandle));
        }

        return WithMaterial(state, material, accepted);
    }

    private static PreparedMaterialLifecycleResult ApplyAttachPayload(
        PreparedMaterialLifecycleState state,
        PreparedMaterialAttachPayload input)
    {
        EnsureMaterialId(input.MaterialId);
        EnsurePayloadHandle(input.PayloadHandle);

        if (!state.Materials.TryGetValue(input.MaterialId, out PreparedMaterialRecord? material))
        {
            return new PreparedMaterialLifecycleResult(
                state,
                [
                    new(
                        PreparedMaterialDecisionReason.StaleMaterialRefused,
                        input.MaterialId),
                    new(
                        PreparedMaterialDecisionReason.PayloadOwnershipRefused,
                        input.MaterialId,
                        input.PayloadHandle),
                ]);
        }

        if (material.Status != PreparedMaterialLifecycleStatus.Preparing)
        {
            PreparedMaterialDecisionReason reason =
                material.Status == PreparedMaterialLifecycleStatus.Terminal
                    ? PreparedMaterialDecisionReason.AlreadyTerminalNoOp
                    : PreparedMaterialDecisionReason.InvalidTransitionRefused;
            return new PreparedMaterialLifecycleResult(
                state,
                [
                    new(reason, input.MaterialId),
                    new(
                        PreparedMaterialDecisionReason.PayloadOwnershipRefused,
                        input.MaterialId,
                        input.PayloadHandle),
                ]);
        }

        PreparedMaterialRecord ready = material with
        {
            Status = PreparedMaterialLifecycleStatus.Ready,
            PayloadHandle = input.PayloadHandle,
        };
        return WithMaterial(
            state,
            ready,
            [
                new(
                    PreparedMaterialDecisionReason.PreparationReady,
                    input.MaterialId,
                    input.PayloadHandle),
                new(
                    PreparedMaterialDecisionReason.PayloadOwnershipAccepted,
                    input.MaterialId,
                    input.PayloadHandle),
            ]);
    }

    private static PreparedMaterialLifecycleResult ApplyReserve(
        PreparedMaterialLifecycleState state,
        PreparedMaterialReserve input)
    {
        EnsureMaterialId(input.MaterialId);
        EnsureReservationId(input.ReservationId);
        ArgumentNullException.ThrowIfNull(input.Context);
        ValidateUseContext(input.Context);

        if (!TryGetActive(
            state,
            input.MaterialId,
            out PreparedMaterialRecord? material,
            out PreparedMaterialLifecycleResult? refused))
        {
            return refused!;
        }

        if (material.Status == PreparedMaterialLifecycleStatus.Reserved)
        {
            return SingleDecision(
                state,
                PreparedMaterialDecisionReason.ReservationConflict,
                input.MaterialId,
                reservationId: input.ReservationId);
        }

        if (material.Status != PreparedMaterialLifecycleStatus.Ready)
        {
            return SingleDecision(
                state,
                PreparedMaterialDecisionReason.InvalidTransitionRefused,
                input.MaterialId);
        }

        if (IsExpired(material.Binding, input.Context.ObservedAt))
        {
            return Terminalize(
                state,
                material,
                PreparedMaterialTerminalReason.Expired,
                PreparedMaterialDecisionReason.Expired);
        }

        if (!BindingMatches(material.Binding, input.Context))
        {
            return SingleDecision(
                state,
                PreparedMaterialDecisionReason.BindingMismatch,
                input.MaterialId,
                reservationId: input.ReservationId);
        }

        PreparedMaterialRecord reserved = material with
        {
            Status = PreparedMaterialLifecycleStatus.Reserved,
            Reservation = new PreparedMaterialReservation(input.ReservationId, input.Context),
        };
        return WithMaterial(
            state,
            reserved,
            [
                new(
                    PreparedMaterialDecisionReason.ReservationAcquired,
                    input.MaterialId,
                    material.PayloadHandle,
                    input.ReservationId),
            ]);
    }
    private static PreparedMaterialLifecycleResult ApplyRelease(
        PreparedMaterialLifecycleState state,
        PreparedMaterialRelease input)
    {
        EnsureMaterialId(input.MaterialId);
        EnsureReservationId(input.ReservationId);
        ArgumentNullException.ThrowIfNull(input.CurrentContext);
        ValidateUseContext(input.CurrentContext);

        if (!TryGetReserved(
            state,
            input.MaterialId,
            input.ReservationId,
            out PreparedMaterialRecord? material,
            out PreparedMaterialLifecycleResult? refused))
        {
            return refused!;
        }

        if (material.ReusePolicy == PreparedMaterialReusePolicy.SingleOpportunity)
        {
            return Terminalize(
                state,
                material,
                PreparedMaterialTerminalReason.Abandoned,
                PreparedMaterialDecisionReason.SingleOpportunityReleaseDiscarded,
                input.ReservationId);
        }

        if (IsExpired(material.Binding, input.CurrentContext.ObservedAt))
        {
            return Terminalize(
                state,
                material,
                PreparedMaterialTerminalReason.Expired,
                PreparedMaterialDecisionReason.Expired,
                input.ReservationId);
        }

        if (!BindingMatches(material.Binding, input.CurrentContext))
        {
            return Terminalize(
                state,
                material,
                PreparedMaterialTerminalReason.BindingInvalidated,
                PreparedMaterialDecisionReason.BindingInvalidated,
                input.ReservationId);
        }

        PreparedMaterialRecord ready = material with
        {
            Status = PreparedMaterialLifecycleStatus.Ready,
            Reservation = null,
        };
        return WithMaterial(
            state,
            ready,
            [
                new(
                    PreparedMaterialDecisionReason.ReservationReleased,
                    input.MaterialId,
                    material.PayloadHandle,
                    input.ReservationId),
            ]);
    }

    private static PreparedMaterialLifecycleResult ApplyComplete(
        PreparedMaterialLifecycleState state,
        PreparedMaterialComplete input)
    {
        EnsureMaterialId(input.MaterialId);
        EnsureReservationId(input.ReservationId);
        ArgumentNullException.ThrowIfNull(input.CurrentContext);
        ValidateUseContext(input.CurrentContext);

        if (!TryGetReserved(
            state,
            input.MaterialId,
            input.ReservationId,
            out PreparedMaterialRecord? material,
            out PreparedMaterialLifecycleResult? refused))
        {
            return refused!;
        }

        if (!ReservationContextMatches(material.Reservation!, input.CurrentContext))
        {
            return SingleDecision(
                state,
                PreparedMaterialDecisionReason.BindingMismatch,
                input.MaterialId,
                reservationId: input.ReservationId);
        }

        if (IsExpired(material.Binding, input.CurrentContext.ObservedAt))
        {
            return Terminalize(
                state,
                material,
                PreparedMaterialTerminalReason.Expired,
                PreparedMaterialDecisionReason.Expired,
                input.ReservationId);
        }

        if (!BindingMatches(material.Binding, input.CurrentContext))
        {
            return Terminalize(
                state,
                material,
                PreparedMaterialTerminalReason.BindingInvalidated,
                PreparedMaterialDecisionReason.BindingInvalidated,
                input.ReservationId);
        }

        if (input.Disposition == PreparedMaterialCompletionDisposition.ReturnToInventory)
        {
            if (material.ReusePolicy != PreparedMaterialReusePolicy.ReusableWithinBinding)
            {
                return SingleDecision(
                    state,
                    PreparedMaterialDecisionReason.InvalidTransitionRefused,
                    input.MaterialId,
                    reservationId: input.ReservationId);
            }

            PreparedMaterialRecord ready = material with
            {
                Status = PreparedMaterialLifecycleStatus.Ready,
                Reservation = null,
            };
            return WithMaterial(
                state,
                ready,
                [
                    new(
                        PreparedMaterialDecisionReason.CompletedReturnedToInventory,
                        input.MaterialId,
                        material.PayloadHandle,
                        input.ReservationId),
                ]);
        }

        if (input.Disposition != PreparedMaterialCompletionDisposition.RetireOrConsume)
        {
            throw new ArgumentOutOfRangeException(nameof(input));
        }

        return Terminalize(
            state,
            material,
            PreparedMaterialTerminalReason.Completed,
            PreparedMaterialDecisionReason.CompletedRetired,
            input.ReservationId);
    }

    private static PreparedMaterialLifecycleResult ApplyTerminate(
        PreparedMaterialLifecycleState state,
        PreparedMaterialTerminate input)
    {
        EnsureMaterialId(input.MaterialId);
        if (input.ReservationId is not null)
        {
            EnsureReservationId(input.ReservationId.Value);
        }

        if (!TryGetActive(
            state,
            input.MaterialId,
            out PreparedMaterialRecord? material,
            out PreparedMaterialLifecycleResult? refused))
        {
            return refused!;
        }

        if (input.Reason is PreparedMaterialTerminalReason.Completed
            or PreparedMaterialTerminalReason.Expired
            or PreparedMaterialTerminalReason.BindingInvalidated)
        {
            return SingleDecision(
                state,
                PreparedMaterialDecisionReason.InvalidTransitionRefused,
                input.MaterialId);
        }

        if (material.Status == PreparedMaterialLifecycleStatus.Reserved)
        {
            if (input.ReservationId is null
                || material.Reservation?.Id != input.ReservationId.Value)
            {
                return SingleDecision(
                    state,
                    PreparedMaterialDecisionReason.StaleReservationRefused,
                    input.MaterialId,
                    reservationId: input.ReservationId);
            }
        }
        else if (input.ReservationId is not null)
        {
            return SingleDecision(
                state,
                PreparedMaterialDecisionReason.InvalidTransitionRefused,
                input.MaterialId,
                reservationId: input.ReservationId);
        }

        PreparedMaterialDecisionReason reason = input.Reason switch
        {
            PreparedMaterialTerminalReason.Abandoned =>
                PreparedMaterialDecisionReason.Abandoned,
            PreparedMaterialTerminalReason.Failed =>
                PreparedMaterialDecisionReason.Failed,
            PreparedMaterialTerminalReason.Discarded =>
                PreparedMaterialDecisionReason.Discarded,
            _ => throw new ArgumentOutOfRangeException(nameof(input)),
        };

        return Terminalize(
            state,
            material,
            input.Reason,
            reason,
            input.ReservationId);
    }

    private static PreparedMaterialLifecycleResult ApplyExpiry(
        PreparedMaterialLifecycleState state,
        PreparedMaterialEvaluateExpiry input)
    {
        EnsureMaterialId(input.MaterialId);
        if (!TryGetActive(
            state,
            input.MaterialId,
            out PreparedMaterialRecord? material,
            out PreparedMaterialLifecycleResult? refused))
        {
            return refused!;
        }

        if (!IsExpired(material.Binding, input.ObservedAt))
        {
            return SingleDecision(
                state,
                PreparedMaterialDecisionReason.ExpiryNotReached,
                input.MaterialId);
        }

        return Terminalize(
            state,
            material,
            PreparedMaterialTerminalReason.Expired,
            PreparedMaterialDecisionReason.Expired,
            material.Reservation?.Id);
    }

    private static PreparedMaterialLifecycleResult ApplyBindingInvalidation(
        PreparedMaterialLifecycleState state,
        PreparedMaterialInvalidateBinding input)
    {
        EnsureMaterialId(input.MaterialId);
        ValidateInvalidation(input);

        if (!TryGetActive(
            state,
            input.MaterialId,
            out PreparedMaterialRecord? material,
            out PreparedMaterialLifecycleResult? refused))
        {
            return refused!;
        }

        if (!InvalidationMatches(material.Binding, input))
        {
            return SingleDecision(
                state,
                PreparedMaterialDecisionReason.BindingMismatch,
                input.MaterialId);
        }

        return Terminalize(
            state,
            material,
            PreparedMaterialTerminalReason.BindingInvalidated,
            PreparedMaterialDecisionReason.BindingInvalidated,
            material.Reservation?.Id);
    }

    private static PreparedMaterialLifecycleResult Terminalize(
        PreparedMaterialLifecycleState state,
        PreparedMaterialRecord material,
        PreparedMaterialTerminalReason terminalReason,
        PreparedMaterialDecisionReason decisionReason,
        PreparedMaterialReservationId? reservationId = null)
    {
        PreparedMaterialRecord terminal = material with
        {
            Status = PreparedMaterialLifecycleStatus.Terminal,
            PayloadHandle = null,
            Reservation = null,
            TerminalReason = terminalReason,
        };
        var decisions = new List<PreparedMaterialDecision>
        {
            new(
                decisionReason,
                material.Id,
                material.PayloadHandle,
                reservationId),
        };
        if (material.PayloadHandle is not null)
        {
            decisions.Add(new PreparedMaterialDecision(
                PreparedMaterialDecisionReason.PayloadDisposalRequested,
                material.Id,
                material.PayloadHandle,
                reservationId));
        }

        return WithMaterial(state, terminal, decisions);
    }
    private static bool TryGetActive(
        PreparedMaterialLifecycleState state,
        PreparedMaterialId materialId,
        [NotNullWhen(true)] out PreparedMaterialRecord? material,
        [NotNullWhen(false)] out PreparedMaterialLifecycleResult? refused)
    {
        if (!state.Materials.TryGetValue(materialId, out material))
        {
            refused = SingleDecision(
                state,
                PreparedMaterialDecisionReason.StaleMaterialRefused,
                materialId);
            return false;
        }

        if (material.Status == PreparedMaterialLifecycleStatus.Terminal)
        {
            refused = SingleDecision(
                state,
                PreparedMaterialDecisionReason.AlreadyTerminalNoOp,
                materialId);
            return false;
        }

        refused = null;
        return true;
    }

    private static bool TryGetReserved(
        PreparedMaterialLifecycleState state,
        PreparedMaterialId materialId,
        PreparedMaterialReservationId reservationId,
        [NotNullWhen(true)] out PreparedMaterialRecord? material,
        [NotNullWhen(false)] out PreparedMaterialLifecycleResult? refused)
    {
        if (!TryGetActive(state, materialId, out material, out refused))
        {
            return false;
        }

        if (material!.Status != PreparedMaterialLifecycleStatus.Reserved)
        {
            refused = SingleDecision(
                state,
                PreparedMaterialDecisionReason.InvalidTransitionRefused,
                materialId,
                reservationId: reservationId);
            return false;
        }

        if (material.Reservation?.Id != reservationId)
        {
            refused = SingleDecision(
                state,
                PreparedMaterialDecisionReason.StaleReservationRefused,
                materialId,
                reservationId: reservationId);
            return false;
        }

        return true;
    }

    private static bool BindingMatches(
        PreparedMaterialBinding binding,
        PreparedMaterialUseContext context)
    {
        if (binding.OpportunityId is not null
            && context.OpportunityId != binding.OpportunityId)
        {
            return false;
        }

        if (binding.SubjectTrack is not null
            && (context.Track is null || !binding.SubjectTrack.Matches(context.Track)))
        {
            return false;
        }

        if (binding.SessionId is not null
            && context.SessionId != binding.SessionId)
        {
            return false;
        }

        return true;
    }

    private static bool ReservationContextMatches(
        PreparedMaterialReservation reservation,
        PreparedMaterialUseContext context)
    {
        return reservation.Context.OpportunityId == context.OpportunityId;
    }

    private static bool InvalidationMatches(
        PreparedMaterialBinding binding,
        PreparedMaterialInvalidateBinding input)
    {
        return input.BindingKind switch
        {
            PreparedMaterialBindingKind.Opportunity =>
                binding.OpportunityId is not null
                && binding.OpportunityId == input.OpportunityId,
            PreparedMaterialBindingKind.Track =>
                binding.SubjectTrack is not null
                && input.Track is not null
                && RadioHistory.SameNormalizedText(
                    binding.SubjectTrack.Title,
                    input.Track.Title)
                && RadioHistory.SameNormalizedText(
                    binding.SubjectTrack.Artist,
                    input.Track.Artist),
            PreparedMaterialBindingKind.Session =>
                binding.SessionId is not null
                && binding.SessionId == input.SessionId,
            _ => throw new ArgumentOutOfRangeException(nameof(input)),
        };
    }

    private static bool IsExpired(
        PreparedMaterialBinding binding,
        DateTimeOffset observedAt)
    {
        return binding.ExpiresAt is not null && observedAt >= binding.ExpiresAt.Value;
    }

    private static void ValidateBinding(
        PreparedMaterialReusePolicy reusePolicy,
        PreparedMaterialBinding binding)
    {
        if (reusePolicy is not PreparedMaterialReusePolicy.SingleOpportunity
            and not PreparedMaterialReusePolicy.ReusableWithinBinding)
        {
            throw new ArgumentOutOfRangeException(nameof(reusePolicy));
        }

        if (reusePolicy == PreparedMaterialReusePolicy.SingleOpportunity
            && (binding.OpportunityId is null || binding.OpportunityId <= 0))
        {
            throw new ArgumentException(
                "Single-opportunity material requires a positive opportunity identity.",
                nameof(binding));
        }

        if (reusePolicy == PreparedMaterialReusePolicy.ReusableWithinBinding
            && binding.OpportunityId is not null)
        {
            throw new ArgumentException(
                "Reusable material cannot have a permanent opportunity binding.",
                nameof(binding));
        }

        if (binding.OpportunityId is <= 0)
        {
            throw new ArgumentException(
                "Opportunity identity must be positive when supplied.",
                nameof(binding));
        }

        if (binding.SessionId is not null)
        {
            EnsureSessionId(binding.SessionId.Value);
        }
    }

    private static void ValidateUseContext(PreparedMaterialUseContext context)
    {
        if (context.OpportunityId is <= 0)
        {
            throw new ArgumentException(
                "Opportunity identity must be positive when supplied.",
                nameof(context));
        }

        if (context.SessionId is not null)
        {
            EnsureSessionId(context.SessionId.Value);
        }
    }

    private static void ValidateInvalidation(PreparedMaterialInvalidateBinding input)
    {
        switch (input.BindingKind)
        {
            case PreparedMaterialBindingKind.Opportunity:
                if (input.OpportunityId is null or <= 0
                    || input.Track is not null
                    || input.SessionId is not null)
                {
                    throw new ArgumentException(
                        "Opportunity invalidation requires only one positive opportunity identity.",
                        nameof(input));
                }

                break;
            case PreparedMaterialBindingKind.Track:
                if (input.Track is null
                    || input.OpportunityId is not null
                    || input.SessionId is not null)
                {
                    throw new ArgumentException(
                        "Track invalidation requires only one track identity.",
                        nameof(input));
                }

                break;
            case PreparedMaterialBindingKind.Session:
                if (input.SessionId is null
                    || input.OpportunityId is not null
                    || input.Track is not null)
                {
                    throw new ArgumentException(
                        "Session invalidation requires only one session identity.",
                        nameof(input));
                }

                EnsureSessionId(input.SessionId.Value);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(input));
        }
    }

    private static PreparedMaterialLifecycleResult WithMaterial(
        PreparedMaterialLifecycleState state,
        PreparedMaterialRecord material,
        IReadOnlyList<PreparedMaterialDecision> decisions)
    {
        var materials = new Dictionary<PreparedMaterialId, PreparedMaterialRecord>(
            state.Materials)
        {
            [material.Id] = material,
        };
        return new PreparedMaterialLifecycleResult(
            new PreparedMaterialLifecycleState(
                new System.Collections.ObjectModel.ReadOnlyDictionary<
                    PreparedMaterialId,
                    PreparedMaterialRecord>(materials)),
            decisions.ToArray());
    }

    private static PreparedMaterialLifecycleResult SingleDecision(
        PreparedMaterialLifecycleState state,
        PreparedMaterialDecisionReason reason,
        PreparedMaterialId materialId,
        PreparedPayloadHandle? payloadHandle = null,
        PreparedMaterialReservationId? reservationId = null)
    {
        return new PreparedMaterialLifecycleResult(
            state,
            [new PreparedMaterialDecision(reason, materialId, payloadHandle, reservationId)]);
    }

    private static void AddPayloadRefusal(
        List<PreparedMaterialDecision> decisions,
        PreparedMaterialId materialId,
        PreparedPayloadHandle? payloadHandle)
    {
        if (payloadHandle is not null)
        {
            decisions.Add(new PreparedMaterialDecision(
                PreparedMaterialDecisionReason.PayloadOwnershipRefused,
                materialId,
                payloadHandle));
        }
    }

    private static void EnsureMaterialId(PreparedMaterialId id)
    {
        if (string.IsNullOrWhiteSpace(id.Value))
        {
            throw new ArgumentException("Material identity is required.", nameof(id));
        }
    }

    private static void EnsurePayloadHandle(PreparedPayloadHandle handle)
    {
        if (string.IsNullOrWhiteSpace(handle.Value))
        {
            throw new ArgumentException("Payload handle is required.", nameof(handle));
        }
    }

    private static void EnsureReservationId(PreparedMaterialReservationId id)
    {
        if (string.IsNullOrWhiteSpace(id.Value))
        {
            throw new ArgumentException("Reservation identity is required.", nameof(id));
        }
    }

    private static void EnsureSessionId(PreparedMaterialSessionId id)
    {
        if (string.IsNullOrWhiteSpace(id.Value))
        {
            throw new ArgumentException("Session identity is required.", nameof(id));
        }
    }
}